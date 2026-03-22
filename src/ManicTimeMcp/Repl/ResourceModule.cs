using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class ResourceModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("resource", resource =>
		{
			MapConfig(resource);
			MapTimelines(resource);
			MapHealth(resource);
			MapGuide(resource);
			MapEnvironment(resource);
			MapDataRange(resource);
		});
	}

	private static void MapConfig(IReplMap resource)
	{
		resource.Map("config", ManicTimeReplHandlers.GetConfigResource)
			.WithDescription("Read the active ManicTime configuration.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapTimelines(IReplMap resource)
	{
		resource.Map("timelines", ManicTimeReplHandlers.GetTimelinesResourceAsync)
			.WithDescription("Read the available timelines resource.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapHealth(IReplMap resource)
	{
		resource.Map("health", ManicTimeReplHandlers.GetHealthResource)
			.WithDescription("Read the current health diagnostics resource.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapGuide(IReplMap resource)
	{
		resource.Map("guide", ManicTimeReplHandlers.GetGuideResource)
			.WithDescription("Read the Repl-first usage guide.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapEnvironment(IReplMap resource)
	{
		resource.Map("environment", ManicTimeReplHandlers.GetEnvironmentResourceAsync)
			.WithDescription("Read the device and runtime environment resource.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapDataRange(IReplMap resource)
	{
		resource.Map("data-range", ManicTimeReplHandlers.GetDataRangeResourceAsync)
			.WithDescription("Read known data boundaries per timeline.")
			.ReadOnly()
			.AsResource();
	}
}
