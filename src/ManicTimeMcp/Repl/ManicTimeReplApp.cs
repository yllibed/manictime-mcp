using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Repl;
using Repl.Mcp;
using System.Reflection;

namespace ManicTimeMcp.Repl;

/// <summary>Creates the Repl-first ManicTime command surface and MCP integration.</summary>
public static class ManicTimeReplApp
{
	/// <summary>Creates the configured Repl application.</summary>
	public static ReplApp Create()
	{
		var app = ReplApp.Create(ConfigureServices).UseDefaultInteractive();

		app.UseMcpServer(ConfigureMcpOptions);

		MapTimelineCommands(app);
		MapActivityCommands(app);
		MapUsageCommands(app);
		MapSummaryCommands(app);
		MapScreenshotCommands(app);
		MapResourceCommands(app);
		MapPromptCommands(app);

		return app;
	}

	/// <summary>Runs the Repl app using hybrid startup semantics.</summary>
	public static ValueTask<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(args);
		var effectiveArgs = args.Length == 0 ? ["mcp", "serve"] : args;
		return Create().RunAsync(effectiveArgs, cancellationToken);
	}

	/// <summary>Builds the MCP server options from the Repl command graph.</summary>
	public static McpServerOptions BuildMcpServerOptions()
	{
		var app = Create();
		var coreProperty = typeof(ReplApp).GetProperty("Core", BindingFlags.Instance | BindingFlags.NonPublic);
		var core = coreProperty?.GetValue(app) as ICoreReplApp
			?? throw new InvalidOperationException("Unable to resolve the Repl core graph for MCP option building.");
		return core.BuildMcpServerOptions(ConfigureMcpOptions);
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		services.AddLogging(builder =>
		{
			builder.ClearProviders();
			builder.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
		});

		services
			.AddManicTimeConfiguration()
			.AddManicTimeDatabase()
			.AddManicTimeScreenshots();

		services.AddSingleton<TimelineTools>();
		services.AddSingleton<ActivityTools>();
		services.AddSingleton<NarrativeTools>();
		services.AddSingleton<ManicTimeResources>();
	}

	private static void ConfigureMcpOptions(ReplMcpServerOptions options)
	{
		options.ServerName = "ManicTime MCP";
		options.ServerVersion = HealthService.GetServerVersion();
		options.ResourceUriScheme = "manictime";
		options.AutoPromoteReadOnlyToResources = false;
	}

	private static void MapTimelineCommands(ReplApp app)
	{
		app.Context("timeline", timeline =>
		{
			timeline.Map("list", ManicTimeReplHandlers.ListTimelinesAsync)
				.WithDescription("List available ManicTime timelines.")
				.WithDetails("Returns every available timeline together with its schema information.")
				.ReadOnly();
		});
	}

	private static void MapActivityCommands(ReplApp app)
	{
		app.Context("activity", activity =>
		{
			activity.Map("list", ManicTimeReplHandlers.ListActivitiesAsync)
				.WithDescription("List activities for a timeline inside a date range.")
				.ReadOnly();

			activity.Map("computer-usage", ManicTimeReplHandlers.ListComputerUsageAsync)
				.WithDescription("List computer usage intervals for a date range.")
				.ReadOnly();

			activity.Map("tags", ManicTimeReplHandlers.ListTagsAsync)
				.WithDescription("List tag activities for a date range.")
				.ReadOnly();
		});
	}

	private static void MapUsageCommands(ReplApp app)
	{
		app.Context("usage", usage =>
		{
			usage.Map("applications", ManicTimeReplHandlers.ListApplicationUsageAsync)
				.WithDescription("Summarize application usage for a date range.")
				.ReadOnly();

			usage.Map("documents", ManicTimeReplHandlers.ListDocumentUsageAsync)
				.WithDescription("Summarize document usage for a date range.")
				.ReadOnly();

			usage.Map("websites", ManicTimeReplHandlers.ListWebsiteUsageAsync)
				.WithDescription("Summarize website usage for a date range.")
				.ReadOnly();
		});
	}

	private static void MapSummaryCommands(ReplApp app)
	{
		app.Context("summary", summary =>
		{
			summary.Map("daily", ManicTimeReplHandlers.BuildDailySummaryAsync)
				.WithDescription("Build a single-day activity summary.")
				.WithDetails("Returns segments, aggregate app data, website insights, and suggested screenshots for the selected date.")
				.ReadOnly();

			summary.Map("narrative", ManicTimeReplHandlers.BuildNarrativeSummaryAsync)
				.WithDescription("Build a narrative of what happened during a date range.")
				.WithDetails("Best suited for day-scale retrospectives and timeline reconstruction.")
				.ReadOnly();

			summary.Map("period", ManicTimeReplHandlers.BuildPeriodSummaryAsync)
				.WithDescription("Build a multi-day summary with patterns and day breakdowns.")
				.ReadOnly();
		});
	}

	private static void MapScreenshotCommands(ReplApp app)
	{
		app.Context("screenshot", screenshot =>
		{
			screenshot.Map("list", ManicTimeReplHandlers.ListScreenshotsAsync)
				.WithDescription("List screenshot metadata for a date-time window.")
				.WithDetails("Returns metadata only. Use screenshot get or screenshot crop to retrieve image bytes.")
				.ReadOnly();

			screenshot.Map("get", ManicTimeReplHandlers.GetScreenshot)
				.WithDescription("Fetch a screenshot payload by reference.")
				.WithDetails("Returns metadata plus thumbnail/full image payloads encoded as base64 text.")
				.ReadOnly();

			screenshot.Map("crop", ManicTimeReplHandlers.CropScreenshot)
				.WithDescription("Crop a screenshot region of interest.")
				.ReadOnly();

			screenshot.Map("save", ManicTimeReplHandlers.SaveScreenshotAsync)
				.WithDescription("Persist a screenshot inside an MCP client root.")
				.WithDetails("Validates output paths against MCP client roots and can optionally save a cropped region.")
				.OpenWorld();
		});
	}

	private static void MapResourceCommands(ReplApp app)
	{
		app.Context("resource", resource =>
		{
			resource.Map("config", ManicTimeReplHandlers.GetConfigResource)
				.WithDescription("Read the active ManicTime configuration.")
				.ReadOnly()
				.AsResource();

			resource.Map("timelines", ManicTimeReplHandlers.GetTimelinesResourceAsync)
				.WithDescription("Read the available timelines resource.")
				.ReadOnly()
				.AsResource();

			resource.Map("health", ManicTimeReplHandlers.GetHealthResource)
				.WithDescription("Read the current health diagnostics resource.")
				.ReadOnly()
				.AsResource();

			resource.Map("guide", ManicTimeReplHandlers.GetGuideResource)
				.WithDescription("Read the Repl-first usage guide.")
				.ReadOnly()
				.AsResource();

			resource.Map("environment", ManicTimeReplHandlers.GetEnvironmentResourceAsync)
				.WithDescription("Read the device and runtime environment resource.")
				.ReadOnly()
				.AsResource();

			resource.Map("data-range", ManicTimeReplHandlers.GetDataRangeResourceAsync)
				.WithDescription("Read known data boundaries per timeline.")
				.ReadOnly()
				.AsResource();
		});
	}

	private static void MapPromptCommands(ReplApp app)
	{
		app.Context("prompt", prompt =>
		{
			prompt.Map("daily-review", ManicTimeReplHandlers.BuildDailyReviewPrompt)
				.WithDescription("Guide a daily review workflow.")
				.AsPrompt();

			prompt.Map("weekly-review", ManicTimeReplHandlers.BuildWeeklyReviewPrompt)
				.WithDescription("Guide a multi-day review workflow.")
				.AsPrompt();

			prompt.Map("screenshot-investigation", ManicTimeReplHandlers.BuildScreenshotInvestigationPrompt)
				.WithDescription("Guide a screenshot-led investigation workflow.")
				.AsPrompt();
		});
	}
}
