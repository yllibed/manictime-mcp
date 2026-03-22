using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class UsageModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("usage", usage =>
		{
			MapApplications(usage);
			MapDocuments(usage);
			MapWebsites(usage);
		});
	}

	private static void MapApplications(IReplMap usage)
	{
		usage.Map(
			"applications",
			(
				ReplDateRange period,
				LimitOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.ListApplicationUsageAsync(
						period,
						options,
						new ActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
						cancellationToken))
			.WithDescription("Summarize application usage for a date range.")
			.ReadOnly();
	}

	private static void MapDocuments(IReplMap usage)
	{
		usage.Map(
			"documents",
			(
				ReplDateRange period,
				LimitOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.ListDocumentUsageAsync(
						period,
						options,
						new ActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
						cancellationToken))
			.WithDescription("Summarize document usage for a date range.")
			.ReadOnly();
	}

	private static void MapWebsites(IReplMap usage)
	{
		usage.Map(
			"websites",
			(
				ReplDateRange period,
				WebsiteUsageOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				IScreenshotService screenshotService,
				IScreenshotRegistry screenshotRegistry,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.ListWebsiteUsageAsync(
						period,
						options,
						new NarrativeTools(activityRepository, timelineRepository, usageRepository, capabilities, screenshotService, screenshotRegistry),
						cancellationToken))
			.WithDescription("Summarize website usage for a date range.")
			.ReadOnly();
	}
}
