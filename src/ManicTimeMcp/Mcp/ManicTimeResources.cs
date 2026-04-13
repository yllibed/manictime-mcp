using System.ComponentModel;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Models;

namespace ManicTimeMcp.Mcp;

/// <summary>Read-only resource operations exposing ManicTime configuration, health, and data.</summary>
public sealed class ManicTimeResources
{
	private readonly IDataDirectoryResolver _resolver;
	private readonly IHealthService _healthService;
	private readonly ITimelineRepository _timelineRepository;
	private readonly IEnvironmentRepository _environmentRepository;
	private readonly IUsageRepository _usageRepository;

	/// <summary>Creates resources with injected services.</summary>
	public ManicTimeResources(
		IDataDirectoryResolver resolver,
		IHealthService healthService,
		ITimelineRepository timelineRepository,
		IEnvironmentRepository environmentRepository,
		IUsageRepository usageRepository)
	{
		_resolver = resolver;
		_healthService = healthService;
		_timelineRepository = timelineRepository;
		_environmentRepository = environmentRepository;
		_usageRepository = usageRepository;
	}

	/// <summary>Returns the current ManicTime configuration.</summary>
	[Description("ManicTime MCP server configuration including data directory and source.")]
	public ConfigResource GetConfig()
	{
		var result = _resolver.Resolve();
		return new ConfigResource
		{
			DataDirectory = result.Path,
			DirectorySource = result.Source.ToString(),
		};
	}

	/// <summary>Returns available ManicTime timelines.</summary>
	[Description("List of all ManicTime timelines with schema types.")]
	public async Task<IReadOnlyList<TimelineDto>> GetTimelinesAsync(CancellationToken cancellationToken)
	{
		return await _timelineRepository.GetTimelinesAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>Returns the current health diagnostic report.</summary>
	[Description("Health diagnostic report for the ManicTime MCP environment.")]
	public HealthReport GetHealth()
	{
		return _healthService.GetHealthReport();
	}

	/// <summary>Returns the model usage guide.</summary>
	[Description("Usage guide for AI models: tool inventory, decision trees, playbooks, and data model explanation.")]
	public static string GetGuide() => GuideContent.Text;

	/// <summary>Returns device and runtime environment information.</summary>
	[Description("Device and runtime information from ManicTime environment data.")]
	public async Task<EnvironmentResource> GetEnvironmentAsync(CancellationToken cancellationToken)
	{
		var environments = await _environmentRepository.GetEnvironmentsAsync(cancellationToken).ConfigureAwait(false);
		return new EnvironmentResource { Environments = environments };
	}

	/// <summary>Returns available data date ranges from timeline summaries.</summary>
	[Description("Available data date ranges per timeline. Useful for knowing data boundaries without querying activities.")]
	public async Task<DataRangeResource> GetDataRangeAsync(CancellationToken cancellationToken)
	{
		var summaries = await _usageRepository.GetTimelineSummariesAsync(cancellationToken).ConfigureAwait(false);
		return new DataRangeResource { TimelineSummaries = summaries };
	}
}
