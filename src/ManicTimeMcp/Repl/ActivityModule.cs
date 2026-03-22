using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp;
using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class ActivityModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("activity", activity =>
		{
			MapList(activity);
			MapComputerUsage(activity);
			MapTags(activity);
		});
	}

	private static void MapList(IReplMap activity)
	{
		activity.Map(
			"list",
			(
				long timelineId,
				ReplDateRange period,
				ActivityListOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.ListActivitiesAsync(
						timelineId,
						period,
						options,
						new ActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
						cancellationToken))
			.WithDescription("List activities for a timeline inside a date range.")
			.ReadOnly();
	}

	private static void MapComputerUsage(IReplMap activity)
	{
		activity.Map(
			"computer-usage",
			(
				ReplDateRange period,
				LimitOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.ListComputerUsageAsync(
						period,
						options,
						new ActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
						cancellationToken))
			.WithDescription("List computer usage intervals for a date range.")
			.ReadOnly();
	}

	private static void MapTags(IReplMap activity)
	{
		activity.Map(
			"tags",
			(
				ReplDateRange period,
				LimitOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.ListTagsAsync(
						period,
						options,
						new ActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
						cancellationToken))
			.WithDescription("List tag activities for a date range.")
			.ReadOnly();
	}
}
