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
		activity.Map("list", ManicTimeReplHandlers.ListActivitiesAsync)
			.WithDescription("List activities for a timeline inside a date range.")
			.WithDetails(
				"Returns raw activity records with start/end times, group metadata, and optional enriched fields. " +
				"Use timeline list first to discover valid timeline IDs. " +
				"Prefer usage applications or summary narrative for high-level overviews; use this for drill-down into specific timelines.")
			.ReadOnly();
	}

	private static void MapComputerUsage(IReplMap activity)
	{
		activity.Map("computer-usage", ManicTimeReplHandlers.ListComputerUsageAsync)
			.WithDescription("List computer usage intervals for a date range.")
			.WithDetails(
				"Returns computer on/off intervals showing when the machine was active. " +
				"Useful for determining availability windows and total active hours before diving into app-level detail.")
			.ReadOnly();
	}

	private static void MapTags(IReplMap activity)
	{
		activity.Map("tags", ManicTimeReplHandlers.ListTagsAsync)
			.WithDescription("List tag activities for a date range.")
			.WithDetails(
				"Returns user-defined ManicTime tags with time aggregates. " +
				"Tags are manual annotations the user applies in ManicTime for categorization (e.g. projects, clients).")
			.ReadOnly();
	}
}
