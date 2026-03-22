using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp;
using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class TimelineModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("timeline", timeline =>
		{
			timeline.Map(
				"list",
				(
					ITimelineRepository timelineRepository,
					CancellationToken cancellationToken) =>
						ManicTimeReplHandlers.ListTimelinesAsync(
							new TimelineTools(timelineRepository),
							cancellationToken))
				.WithDescription("List available ManicTime timelines.")
				.WithDetails("Returns every available timeline together with its schema information.")
				.ReadOnly();
		});
	}
}
