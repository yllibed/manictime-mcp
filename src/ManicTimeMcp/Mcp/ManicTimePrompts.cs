using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;

namespace ManicTimeMcp.Mcp;

/// <summary>MCP prompts for common ManicTime workflows.</summary>
[McpServerPromptType]
public sealed class ManicTimePrompts
{
	/// <summary>Generates a daily review prompt for the given date.</summary>
	[McpServerPrompt(Name = "daily_review"), Description("Summarize my activities for a specific date.")]
	public static string DailyReview(
		[Description("Date to review (ISO-8601, e.g. 2025-01-15)")] string date)
	{
		var parsed = DateTime.ParseExact(date, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		var nextDay = parsed.AddDays(1).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

		return $"""
			Use get_activity_narrative with startDate={date}, endDate={nextDay}, and includeSummary=true to retrieve activity data with top-app/top-website aggregates. Synthesize a concise daily summary highlighting top applications, total active time, and notable patterns. Prefer resolved names/colors over internal refs. If the response contains suggestedScreenshots, use get_screenshot to fetch 2-3 of them. After inspecting each thumbnail, use crop_screenshot to extract the most relevant region (e.g. the active window or focused content area). Use save_screenshot to persist the best crops to the project assets folder, then include them in your report — cropped details are sharper and more meaningful than full-screen captures.
			""";
	}

	/// <summary>Generates a weekly review prompt for the given date range.</summary>
	[McpServerPrompt(Name = "weekly_review"), Description("Summarize my week for a date range.")]
	public static string WeeklyReview(
		[Description("Start date (ISO-8601, e.g. 2025-01-13)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-20)")] string endDate)
	{
		return $"""
			Use get_period_summary with startDate={startDate} and endDate={endDate} to retrieve multi-day activity data. Synthesize a weekly overview including busiest/quietest days, top applications and websites, day-of-week patterns, and total active hours. Prefer resolved labels in the final user-facing answer. For each notable day, consider using get_daily_summary then get_screenshot on suggested screenshots. Use crop_screenshot to extract the active window or key content area from each, and save_screenshot to persist the best crops — cropped regions are more readable and informative than full-screen captures.
			""";
	}

	/// <summary>Generates a screenshot investigation prompt for a specific datetime.</summary>
	[McpServerPrompt(Name = "screenshot_investigation"), Description("What was I doing at a specific time? Investigates via screenshots and activity data.")]
	public static string ScreenshotInvestigation(
		[Description("Datetime to investigate (ISO-8601, e.g. 2025-01-15T15:00:00)")] string datetime)
	{
		return $"""
			Use list_screenshots to find screenshots near {datetime} (within a 5-minute window). Use get_screenshot to retrieve the most relevant screenshot — you will receive a low-resolution thumbnail you can inspect. If you spot a region that needs more detail, call crop_screenshot with percentage ROI coordinates to get a full-resolution crop you can analyze. Combine visual findings with get_activity_narrative for the same period, then produce a user-facing explanation using resolved names rather than internal refs.
			""";
	}
}
