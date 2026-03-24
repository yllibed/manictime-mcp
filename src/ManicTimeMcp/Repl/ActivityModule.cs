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
			.ReadOnly();
	}

	private static void MapComputerUsage(IReplMap activity)
	{
		activity.Map("computer-usage", ManicTimeReplHandlers.ListComputerUsageAsync)
			.WithDescription("List computer usage intervals for a date range.")
			.ReadOnly();
	}

	private static void MapTags(IReplMap activity)
	{
		activity.Map("tags", ManicTimeReplHandlers.ListTagsAsync)
			.WithDescription("List tag activities for a date range.")
			.ReadOnly();
	}
}
