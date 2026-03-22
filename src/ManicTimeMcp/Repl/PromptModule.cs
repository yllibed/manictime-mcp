using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class PromptModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("prompt", prompt =>
		{
			prompt.Map(
				"daily-review",
				(DateOnly date) => ManicTimeReplHandlers.BuildDailyReviewPrompt(date))
				.WithDescription("Guide a daily review workflow.")
				.AsPrompt();

			prompt.Map(
				"weekly-review",
				(ReplDateRange period) => ManicTimeReplHandlers.BuildWeeklyReviewPrompt(period))
				.WithDescription("Guide a multi-day review workflow.")
				.AsPrompt();

			prompt.Map(
				"screenshot-investigation",
				(ReplDateTimeRange window) => ManicTimeReplHandlers.BuildScreenshotInvestigationPrompt(window))
				.WithDescription("Guide a screenshot-led investigation workflow.")
				.AsPrompt();
		});
	}
}
