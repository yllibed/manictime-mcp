using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class SummaryModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("summary", summary =>
		{
			MapDaily(summary);
			MapNarrative(summary);
			MapPeriod(summary);
		});
	}

	private static void MapDaily(IReplMap summary)
	{
		summary.Map(
			"daily",
			(
				DateOnly date,
				DailySummaryOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				IScreenshotService screenshotService,
				IScreenshotRegistry screenshotRegistry,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.BuildDailySummaryAsync(
						date,
						options,
						new NarrativeTools(activityRepository, timelineRepository, usageRepository, capabilities, screenshotService, screenshotRegistry),
						cancellationToken))
			.WithDescription("Build a single-day activity summary.")
			.WithDetails("Returns segments, aggregate app data, website insights, and suggested screenshots for the selected date.")
			.ReadOnly();
	}

	private static void MapNarrative(IReplMap summary)
	{
		summary.Map(
			"narrative",
			(
				ReplDateRange period,
				NarrativeSummaryOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				IScreenshotService screenshotService,
				IScreenshotRegistry screenshotRegistry,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.BuildNarrativeSummaryAsync(
						period,
						options,
						new NarrativeTools(activityRepository, timelineRepository, usageRepository, capabilities, screenshotService, screenshotRegistry),
						cancellationToken))
			.WithDescription("Build a narrative of what happened during a date range.")
			.WithDetails("Best suited for day-scale retrospectives and timeline reconstruction.")
			.ReadOnly();
	}

	private static void MapPeriod(IReplMap summary)
	{
		summary.Map(
			"period",
			(
				ReplDateRange period,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				IScreenshotService screenshotService,
				IScreenshotRegistry screenshotRegistry,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.BuildPeriodSummaryAsync(
						period,
						new NarrativeTools(activityRepository, timelineRepository, usageRepository, capabilities, screenshotService, screenshotRegistry),
						cancellationToken))
			.WithDescription("Build a multi-day summary with patterns and day breakdowns.")
			.ReadOnly();
	}
}
