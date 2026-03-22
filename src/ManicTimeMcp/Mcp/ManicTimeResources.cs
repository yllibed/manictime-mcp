using System.ComponentModel;
using System.Text.Json;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Screenshots;

namespace ManicTimeMcp.Mcp;

/// <summary>Read-only resource operations exposing ManicTime configuration, health, and data.</summary>
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
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
	public string GetConfig()
	{
		var result = _resolver.Resolve();
		return JsonSerializer.Serialize(new
		{
			dataDirectory = result.Path,
			directorySource = result.Source.ToString(),
		}, JsonOptions.Default);
	}

	/// <summary>Returns available ManicTime timelines.</summary>
	[Description("List of all ManicTime timelines with schema types.")]
	public async Task<string> GetTimelinesAsync(CancellationToken cancellationToken)
	{
		var timelines = await _timelineRepository.GetTimelinesAsync(cancellationToken).ConfigureAwait(false);
		return JsonSerializer.Serialize(timelines, JsonOptions.Default);
	}

	/// <summary>Returns the current health diagnostic report.</summary>
	[Description("Health diagnostic report for the ManicTime MCP environment.")]
	public string GetHealth()
	{
		var report = _healthService.GetHealthReport();
		return JsonSerializer.Serialize(report, JsonOptions.Default);
	}

	/// <summary>Returns the model usage guide.</summary>
	[Description("Usage guide for AI models: tool inventory, decision trees, playbooks, and data model explanation.")]
	public static string GetGuide() => GuideContent.Text;

	/// <summary>Returns device and runtime environment information.</summary>
	[Description("Device and runtime information from ManicTime environment data.")]
	public async Task<string> GetEnvironmentAsync(CancellationToken cancellationToken)
	{
		var environments = await _environmentRepository.GetEnvironmentsAsync(cancellationToken).ConfigureAwait(false);
		return JsonSerializer.Serialize(new { environments }, JsonOptions.Default);
	}

	/// <summary>Returns available data date ranges from timeline summaries.</summary>
	[Description("Available data date ranges per timeline. Useful for knowing data boundaries without querying activities.")]
	public async Task<string> GetDataRangeAsync(CancellationToken cancellationToken)
	{
		var summaries = await _usageRepository.GetTimelineSummariesAsync(cancellationToken).ConfigureAwait(false);
		return JsonSerializer.Serialize(new { timelineSummaries = summaries }, JsonOptions.Default);
	}
}
#pragma warning restore IL2026
