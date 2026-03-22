using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp;
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
		resource.Map(
			"config",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository) =>
					ManicTimeReplHandlers.GetConfigResource(
						new ManicTimeResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository)))
			.WithDescription("Read the active ManicTime configuration.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapTimelines(IReplMap resource)
	{
		resource.Map(
			"timelines",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.GetTimelinesResourceAsync(
						new ManicTimeResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository),
						cancellationToken))
			.WithDescription("Read the available timelines resource.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapHealth(IReplMap resource)
	{
		resource.Map(
			"health",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository) =>
					ManicTimeReplHandlers.GetHealthResource(
						new ManicTimeResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository)))
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
		resource.Map(
			"environment",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.GetEnvironmentResourceAsync(
						new ManicTimeResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository),
						cancellationToken))
			.WithDescription("Read the device and runtime environment resource.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapDataRange(IReplMap resource)
	{
		resource.Map(
			"data-range",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.GetDataRangeResourceAsync(
						new ManicTimeResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository),
						cancellationToken))
			.WithDescription("Read known data boundaries per timeline.")
			.ReadOnly()
			.AsResource();
	}
}
