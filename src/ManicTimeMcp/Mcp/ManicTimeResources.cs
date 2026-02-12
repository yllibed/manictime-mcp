using System.ComponentModel;
using System.Text.Json;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ModelContextProtocol.Server;

namespace ManicTimeMcp.Mcp;

/// <summary>MCP resources exposing ManicTime configuration and health.</summary>
[McpServerResourceType]
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
public sealed class ManicTimeResources
{
	private readonly IDataDirectoryResolver _resolver;
	private readonly IHealthService _healthService;
	private readonly ITimelineRepository _timelineRepository;

	/// <summary>Creates resources with injected services.</summary>
	public ManicTimeResources(
		IDataDirectoryResolver resolver,
		IHealthService healthService,
		ITimelineRepository timelineRepository)
	{
		_resolver = resolver;
		_healthService = healthService;
		_timelineRepository = timelineRepository;
	}

	/// <summary>Returns the current ManicTime configuration.</summary>
	[McpServerResource(UriTemplate = "manictime://config"), Description("ManicTime MCP server configuration including data directory and source.")]
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
	[McpServerResource(UriTemplate = "manictime://timelines"), Description("List of all ManicTime timelines with schema types.")]
	public async Task<string> GetTimelinesAsync(CancellationToken cancellationToken)
	{
		var timelines = await _timelineRepository.GetTimelinesAsync(cancellationToken).ConfigureAwait(false);
		return JsonSerializer.Serialize(timelines, JsonOptions.Default);
	}

	/// <summary>Returns the current health diagnostic report.</summary>
	[McpServerResource(UriTemplate = "manictime://health"), Description("Health diagnostic report for the ManicTime MCP environment.")]
	public string GetHealth()
	{
		var report = _healthService.GetHealthReport();
		return JsonSerializer.Serialize(report, JsonOptions.Default);
	}
}
#pragma warning restore IL2026
