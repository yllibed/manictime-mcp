using ManicTimeMcp.Mcp.Models;

namespace ManicTimeMcp.Mcp;

/// <summary>Selects the most representative screenshots from narrative segments.</summary>
internal static class ScreenshotSuggestionSelector
{
	private const int MaxSuggestions = 5;
	private static readonly TimeSpan ProximityWindow = TimeSpan.FromMinutes(5);

	/// <summary>
	/// Selects up to <see cref="MaxSuggestions"/> curated screenshots from merged segments.
	/// Candidates come from app transitions and the longest sessions.
	/// </summary>
	/// <returns>A list of suggestions sorted by timestamp, or null if none available.</returns>
	internal static List<SuggestedScreenshot>? Select(List<NarrativeSegment> segments)
	{
		if (segments.Count == 0)
		{
			return null;
		}

		var candidates = CollectCandidates(segments);
		if (candidates.Count == 0)
		{
			return null;
		}

		var deduped = DeduplicateCandidates(candidates);
		if (deduped.Count == 0)
		{
			return null;
		}

		return deduped
			.Take(MaxSuggestions)
			.Select(c => new SuggestedScreenshot
			{
				ScreenshotRef = c.Segment.ScreenshotRef!,
				Timestamp = c.Segment.Start,
				Application = c.Segment.Application,
				Hint = c.Hint,
			})
			.ToList();
	}

	private static List<(NarrativeSegment Segment, string Hint)> CollectCandidates(
		List<NarrativeSegment> segments)
	{
		var candidates = new List<(NarrativeSegment Segment, string Hint)>();

		// App-transition candidates (where application differs from previous)
		for (var i = 1; i < segments.Count; i++)
		{
			if (!string.Equals(segments[i].Application, segments[i - 1].Application, StringComparison.Ordinal)
				&& segments[i].ScreenshotRef is not null)
			{
				candidates.Add((segments[i], "app transition"));
			}
		}

		// Long-session candidates (top segments by duration)
		var longSessions = segments
			.Where(s => s.ScreenshotRef is not null)
			.OrderByDescending(s => s.DurationMinutes)
			.Take(MaxSuggestions);

		foreach (var seg in longSessions)
		{
			candidates.Add((seg, "long session"));
		}

		return candidates;
	}

	private static List<(NarrativeSegment Segment, string Hint)> DeduplicateCandidates(
		List<(NarrativeSegment Segment, string Hint)> candidates)
	{
		candidates.Sort((a, b) => string.Compare(a.Segment.Start, b.Segment.Start, StringComparison.Ordinal));

		var seen = new HashSet<string>(StringComparer.Ordinal);
		var deduped = new List<(NarrativeSegment Segment, string Hint)>();

		foreach (var (segment, hint) in candidates)
		{
			var screenshotRef = segment.ScreenshotRef!;
			if (!seen.Add(screenshotRef))
			{
				continue;
			}

			if (deduped.Count > 0 && IsWithinProximityWindow(deduped[^1].Segment.Start, segment.Start))
			{
				continue;
			}

			deduped.Add((segment, hint));
		}

		return deduped;
	}

	private static bool IsWithinProximityWindow(string startA, string startB)
	{
		if (!DateTime.TryParseExact(startA, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture,
				System.Globalization.DateTimeStyles.None, out var dtA)
			|| !DateTime.TryParseExact(startB, "yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture,
				System.Globalization.DateTimeStyles.None, out var dtB))
		{
			return false;
		}

		return (dtB - dtA).Duration() < ProximityWindow;
	}
}
