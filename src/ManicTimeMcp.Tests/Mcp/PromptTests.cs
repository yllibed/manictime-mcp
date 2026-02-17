using AwesomeAssertions;
using ManicTimeMcp.Mcp;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class PromptTests
{
	private static McpTestHarness CreateHarness()
	{
		return new McpTestHarness((_, builder) =>
		{
			builder.WithPrompts<ManicTimePrompts>();
		});
	}

	[TestMethod]
	public async Task ListPrompts_ContainsAllExpectedPrompts()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var prompts = await client.ListPromptsAsync().ConfigureAwait(false);

		var names = prompts.Select(p => p.Name).ToList();
		names.Should().Contain("daily_review");
		names.Should().Contain("weekly_review");
		names.Should().Contain("screenshot_investigation");
	}

	[TestMethod]
	public async Task DailyReview_ReturnsPromptText()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.GetPromptAsync(
			"daily_review",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["date"] = "2025-01-15",
			}).ConfigureAwait(false);

		result.Messages.Should().ContainSingle();
		var text = ((TextContentBlock)result.Messages[0].Content).Text;
		text.Should().Contain("get_activity_narrative");
		text.Should().Contain("startDate=2025-01-15");
		text.Should().Contain("endDate=2025-01-16");
	}

	[TestMethod]
	public async Task WeeklyReview_ReturnsPromptText()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.GetPromptAsync(
			"weekly_review",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-13",
				["endDate"] = "2025-01-20",
			}).ConfigureAwait(false);

		result.Messages.Should().ContainSingle();
		var text = ((TextContentBlock)result.Messages[0].Content).Text;
		text.Should().Contain("get_period_summary");
		text.Should().Contain("startDate=2025-01-13");
		text.Should().Contain("endDate=2025-01-20");
	}

	[TestMethod]
	public async Task ScreenshotInvestigation_ReturnsPromptText()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.GetPromptAsync(
			"screenshot_investigation",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["datetime"] = "2025-01-15T15:00:00",
			}).ConfigureAwait(false);

		result.Messages.Should().ContainSingle();
		var text = ((TextContentBlock)result.Messages[0].Content).Text;
		text.Should().Contain("list_screenshots");
		text.Should().Contain("crop_screenshot");
		text.Should().Contain("2025-01-15T15:00:00");
	}
}
