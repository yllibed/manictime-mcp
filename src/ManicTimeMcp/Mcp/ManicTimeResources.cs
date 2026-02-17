using System.ComponentModel;
using System.Text.Json;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Screenshots;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ManicTimeMcp.Mcp;

/// <summary>MCP resources exposing ManicTime configuration, health, and data.</summary>
[McpServerResourceType]
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
public sealed class ManicTimeResources
{
	private readonly IDataDirectoryResolver _resolver;
	private readonly IHealthService _healthService;
	private readonly ITimelineRepository _timelineRepository;
	private readonly IEnvironmentRepository _environmentRepository;
	private readonly IUsageRepository _usageRepository;
	private readonly IScreenshotRegistry _screenshotRegistry;
	private readonly IScreenshotService _screenshotService;

	/// <summary>Creates resources with injected services.</summary>
	public ManicTimeResources(
		IDataDirectoryResolver resolver,
		IHealthService healthService,
		ITimelineRepository timelineRepository,
		IEnvironmentRepository environmentRepository,
		IUsageRepository usageRepository,
		IScreenshotRegistry screenshotRegistry,
		IScreenshotService screenshotService)
	{
		_resolver = resolver;
		_healthService = healthService;
		_timelineRepository = timelineRepository;
		_environmentRepository = environmentRepository;
		_usageRepository = usageRepository;
		_screenshotRegistry = screenshotRegistry;
		_screenshotService = screenshotService;
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

	/// <summary>Returns the model usage guide.</summary>
	[McpServerResource(UriTemplate = "manictime://guide"), Description("Usage guide for AI models: tool inventory, decision trees, playbooks, and data model explanation.")]
	public static string GetGuide() => GuideContent.Text;

	/// <summary>Returns device and runtime environment information.</summary>
	[McpServerResource(UriTemplate = "manictime://environment"), Description("Device and runtime information from ManicTime environment data.")]
	public async Task<string> GetEnvironmentAsync(CancellationToken cancellationToken)
	{
		var environments = await _environmentRepository.GetEnvironmentsAsync(cancellationToken).ConfigureAwait(false);
		return JsonSerializer.Serialize(new { environments }, JsonOptions.Default);
	}

	/// <summary>Returns available data date ranges from timeline summaries.</summary>
	[McpServerResource(UriTemplate = "manictime://data-range"), Description("Available data date ranges per timeline. Useful for knowing data boundaries without querying activities.")]
	public async Task<string> GetDataRangeAsync(CancellationToken cancellationToken)
	{
		var summaries = await _usageRepository.GetTimelineSummariesAsync(cancellationToken).ConfigureAwait(false);
		return JsonSerializer.Serialize(new { timelineSummaries = summaries }, JsonOptions.Default);
	}

	/// <summary>Returns a screenshot by reference for lazy-fetch resolution.</summary>
	[McpServerResource(UriTemplate = "manictime://screenshot/{screenshotRef}", MimeType = "image/jpeg"), Description("Lazy-fetch screenshot resource. Resolve via screenshotRef obtained from list_screenshots.")]
	public IEnumerable<ResourceContents> GetScreenshot(string screenshotRef)
	{
		var info = _screenshotRegistry.TryResolve(screenshotRef);
		if (info is null)
		{
			yield return new TextResourceContents
			{
				Uri = $"manictime://screenshot/{screenshotRef}",
				Text = JsonSerializer.Serialize(new { error = "Unknown screenshotRef" }, JsonOptions.Default),
				MimeType = "application/json",
			};
			yield break;
		}

		var bytes = _screenshotService.ReadScreenshot(info.FilePath);
		if (bytes is not null)
		{
			yield return new BlobResourceContents
			{
				Uri = $"manictime://screenshot/{screenshotRef}",
				Blob = Convert.ToBase64String(bytes),
				MimeType = "image/jpeg",
			};
		}

		yield return new TextResourceContents
		{
			Uri = $"manictime://screenshot/{screenshotRef}",
			Text = JsonSerializer.Serialize(new
			{
				screenshotRef,
				timestamp = info.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture),
				info.Width,
				info.Height,
				info.Monitor,
				info.IsThumbnail,
			}, JsonOptions.Default),
			MimeType = "application/json",
		};
	}
}
#pragma warning restore IL2026
