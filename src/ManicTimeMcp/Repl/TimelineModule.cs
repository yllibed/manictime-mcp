using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class TimelineModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("timeline", timeline =>
		{
			timeline.Map("list", ManicTimeReplHandlers.ListTimelinesAsync)
				.WithDescription("List available ManicTime timelines.")
				.WithDetails("Returns every available timeline together with its schema information.")
				.ReadOnly();
		});
	}
}
