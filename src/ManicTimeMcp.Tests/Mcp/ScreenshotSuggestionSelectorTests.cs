using AwesomeAssertions;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Mcp.Models;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class ScreenshotSuggestionSelectorTests
{
	private static NarrativeSegment MakeSegment(
		string start, string end, string app, string? screenshotRef)
	{
		var startDt = DateTime.ParseExact(start, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
		var endDt = DateTime.ParseExact(end, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
		return new NarrativeSegment
		{
			Start = start,
			End = end,
			DurationMinutes = Math.Round((endDt - startDt).TotalMinutes, digits: 1),
			Application = app,
			ScreenshotRef = screenshotRef,
		};
	}

	[TestMethod]
	public void Select_WithTransitions_ReturnsCandidates()
	{
		var segments = new List<NarrativeSegment>
		{
			MakeSegment("2025-01-15 08:00:00", "2025-01-15 09:00:00", "VS Code", "ref-0800"),
			MakeSegment("2025-01-15 09:00:00", "2025-01-15 10:00:00", "Chrome", "ref-0900"),
			MakeSegment("2025-01-15 10:00:00", "2025-01-15 11:00:00", "Slack", "ref-1000"),
		};

		var result = ScreenshotSuggestionSelector.Select(segments);

		result.Should().NotBeNull();
		result!.Count.Should().BeGreaterThanOrEqualTo(1);
		result.Should().Contain(s => s.Hint == "app transition");
	}

	[TestMethod]
	public void Select_WithLongSegments_ReturnsCandidates()
	{
		// All same app — no transitions, but long sessions should produce candidates
		var segments = new List<NarrativeSegment>
		{
			MakeSegment("2025-01-15 08:00:00", "2025-01-15 12:00:00", "VS Code", "ref-0800"),
			MakeSegment("2025-01-15 13:00:00", "2025-01-15 15:00:00", "VS Code", "ref-1300"),
		};

		var result = ScreenshotSuggestionSelector.Select(segments);

		result.Should().NotBeNull();
		result!.Should().Contain(s => s.Hint == "long session");
	}

	[TestMethod]
	public void Select_DeduplicatesWithinWindow()
	{
		// Two transitions within 3 minutes of each other — should deduplicate
		var segments = new List<NarrativeSegment>
		{
			MakeSegment("2025-01-15 08:00:00", "2025-01-15 08:02:00", "VS Code", "ref-0800"),
			MakeSegment("2025-01-15 08:02:00", "2025-01-15 08:03:00", "Chrome", "ref-0802"),
			MakeSegment("2025-01-15 08:03:00", "2025-01-15 08:04:00", "Slack", "ref-0803"),
			MakeSegment("2025-01-15 09:00:00", "2025-01-15 10:00:00", "Teams", "ref-0900"),
		};

		var result = ScreenshotSuggestionSelector.Select(segments);

		result.Should().NotBeNull();
		// The 08:02 and 08:03 candidates are within 5-min window; one should be deduplicated
		var timestamps = result!.Select(s => s.Timestamp).ToList();
		// Should not have both 08:02:00 and 08:03:00
		var earlyWindow = timestamps.Count(t => t.StartsWith("2025-01-15 08:0", StringComparison.Ordinal));
		earlyWindow.Should().BeLessThanOrEqualTo(1);
	}

	[TestMethod]
	public void Select_CapsAtMaxCount()
	{
		// Create many transitions to exceed the cap
		var segments = new List<NarrativeSegment>();
		for (var i = 0; i < 15; i++)
		{
			var hour = 6 + i; // spread across hours to avoid proximity dedup (6-20)
			var start = $"2025-01-15 {hour:D2}:00:00";
			var end = $"2025-01-15 {hour:D2}:30:00";
			segments.Add(MakeSegment(start, end, $"App{i}", $"ref-{hour:D2}00"));
		}

		var result = ScreenshotSuggestionSelector.Select(segments);

		result.Should().NotBeNull();
		result!.Count.Should().BeLessThanOrEqualTo(5);
	}

	[TestMethod]
	public void Select_NoScreenshotRefs_ReturnsNull()
	{
		var segments = new List<NarrativeSegment>
		{
			MakeSegment("2025-01-15 08:00:00", "2025-01-15 09:00:00", "VS Code", screenshotRef: null),
			MakeSegment("2025-01-15 09:00:00", "2025-01-15 10:00:00", "Chrome", screenshotRef: null),
		};

		var result = ScreenshotSuggestionSelector.Select(segments);

		result.Should().BeNull();
	}

	[TestMethod]
	public void Select_EmptySegments_ReturnsNull()
	{
		var result = ScreenshotSuggestionSelector.Select([]);

		result.Should().BeNull();
	}
}
