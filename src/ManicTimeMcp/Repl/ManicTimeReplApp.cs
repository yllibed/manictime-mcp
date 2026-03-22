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
	public static ReplApp Create(Action<IServiceCollection>? configureServices = null)
	{
		var app = ReplApp.Create(services => ConfigureServices(services, configureServices)).UseDefaultInteractive();

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
	public static McpServerOptions BuildMcpServerOptions(Action<IServiceCollection>? configureServices = null) =>
		BuildMcpServerOptions(Create(configureServices));

	/// <summary>Builds the MCP server options from an existing Repl app.</summary>
	public static McpServerOptions BuildMcpServerOptions(ReplApp app)
	{
		ArgumentNullException.ThrowIfNull(app);
		var coreProperty = typeof(ReplApp).GetProperty("Core", BindingFlags.Instance | BindingFlags.NonPublic);
		var core = coreProperty?.GetValue(app) as ICoreReplApp
			?? throw new InvalidOperationException("Unable to resolve the Repl core graph for MCP option building.");
		return core.BuildMcpServerOptions(ConfigureMcpOptions, GetServiceProvider(app));
	}

	/// <summary>Resolves the shared service provider used by the Repl app.</summary>
	internal static IServiceProvider GetServiceProvider(ReplApp app)
	{
		ArgumentNullException.ThrowIfNull(app);
		var ensureSharedProvider = typeof(ReplApp).GetMethod("EnsureSharedProvider", BindingFlags.Instance | BindingFlags.NonPublic);
		return ensureSharedProvider?.Invoke(app, parameters: null) as IServiceProvider
			?? throw new InvalidOperationException("Unable to resolve the Repl app service provider.");
	}

	private static void ConfigureServices(IServiceCollection services, Action<IServiceCollection>? configureServices)
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

		configureServices?.Invoke(services);
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
			timeline.Map(
				"list",
				(
					ITimelineRepository timelineRepository,
					CancellationToken cancellationToken) =>
						ManicTimeReplHandlers.ListTimelinesAsync(
							CreateTimelineTools(timelineRepository),
							cancellationToken))
				.WithDescription("List available ManicTime timelines.")
				.WithDetails("Returns every available timeline together with its schema information.")
				.ReadOnly();
		});
	}

	private static void MapActivityCommands(ReplApp app)
	{
		app.Context("activity", activity =>
		{
			activity.Map(
				"list",
				(
					long timelineId,
					ReplDateRange period,
					ActivityListOptions options,
					IActivityRepository activityRepository,
					ITimelineRepository timelineRepository,
					IUsageRepository usageRepository,
					QueryCapabilityMatrix capabilities,
					CancellationToken cancellationToken) =>
						ManicTimeReplHandlers.ListActivitiesAsync(
							timelineId,
							period,
							options,
							CreateActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
							cancellationToken))
				.WithDescription("List activities for a timeline inside a date range.")
				.ReadOnly();

			activity.Map(
				"computer-usage",
				(
					ReplDateRange period,
					LimitOptions options,
					IActivityRepository activityRepository,
					ITimelineRepository timelineRepository,
					IUsageRepository usageRepository,
					QueryCapabilityMatrix capabilities,
					CancellationToken cancellationToken) =>
						ManicTimeReplHandlers.ListComputerUsageAsync(
							period,
							options,
							CreateActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
							cancellationToken))
				.WithDescription("List computer usage intervals for a date range.")
				.ReadOnly();

			activity.Map(
				"tags",
				(
					ReplDateRange period,
					LimitOptions options,
					IActivityRepository activityRepository,
					ITimelineRepository timelineRepository,
					IUsageRepository usageRepository,
					QueryCapabilityMatrix capabilities,
					CancellationToken cancellationToken) =>
						ManicTimeReplHandlers.ListTagsAsync(
							period,
							options,
							CreateActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
							cancellationToken))
				.WithDescription("List tag activities for a date range.")
				.ReadOnly();
		});
	}

	private static void MapUsageCommands(ReplApp app)
	{
		app.Context("usage", usage =>
		{
			usage.Map(
				"applications",
				(
					ReplDateRange period,
					LimitOptions options,
					IActivityRepository activityRepository,
					ITimelineRepository timelineRepository,
					IUsageRepository usageRepository,
					QueryCapabilityMatrix capabilities,
					CancellationToken cancellationToken) =>
						ManicTimeReplHandlers.ListApplicationUsageAsync(
							period,
							options,
							CreateActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
							cancellationToken))
				.WithDescription("Summarize application usage for a date range.")
				.ReadOnly();

			usage.Map(
				"documents",
				(
					ReplDateRange period,
					LimitOptions options,
					IActivityRepository activityRepository,
					ITimelineRepository timelineRepository,
					IUsageRepository usageRepository,
					QueryCapabilityMatrix capabilities,
					CancellationToken cancellationToken) =>
						ManicTimeReplHandlers.ListDocumentUsageAsync(
							period,
							options,
							CreateActivityTools(activityRepository, timelineRepository, usageRepository, capabilities),
							cancellationToken))
				.WithDescription("Summarize document usage for a date range.")
				.ReadOnly();

			usage.Map(
				"websites",
				(
					ReplDateRange period,
					WebsiteUsageOptions options,
					IActivityRepository activityRepository,
					ITimelineRepository timelineRepository,
					IUsageRepository usageRepository,
					QueryCapabilityMatrix capabilities,
					IScreenshotService screenshotService,
					IScreenshotRegistry screenshotRegistry,
					CancellationToken cancellationToken) =>
						ManicTimeReplHandlers.ListWebsiteUsageAsync(
							period,
							options,
							CreateNarrativeTools(activityRepository, timelineRepository, usageRepository, capabilities, screenshotService, screenshotRegistry),
							cancellationToken))
				.WithDescription("Summarize website usage for a date range.")
				.ReadOnly();
		});
	}

	private static void MapSummaryCommands(ReplApp app)
	{
		app.Context("summary", summary =>
		{
			MapSummaryDailyCommand(summary);
			MapSummaryNarrativeCommand(summary);
			MapSummaryPeriodCommand(summary);
		});
	}

	private static void MapSummaryDailyCommand(IReplMap summary)
	{
		summary.Map(
			"daily",
			(
				DateOnly date,
				DailySummaryOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				IScreenshotService screenshotService,
				IScreenshotRegistry screenshotRegistry,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.BuildDailySummaryAsync(
						date,
						options,
						CreateNarrativeTools(activityRepository, timelineRepository, usageRepository, capabilities, screenshotService, screenshotRegistry),
						cancellationToken))
			.WithDescription("Build a single-day activity summary.")
			.WithDetails("Returns segments, aggregate app data, website insights, and suggested screenshots for the selected date.")
			.ReadOnly();
	}

	private static void MapSummaryNarrativeCommand(IReplMap summary)
	{
		summary.Map(
			"narrative",
			(
				ReplDateRange period,
				NarrativeSummaryOptions options,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				IScreenshotService screenshotService,
				IScreenshotRegistry screenshotRegistry,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.BuildNarrativeSummaryAsync(
						period,
						options,
						CreateNarrativeTools(activityRepository, timelineRepository, usageRepository, capabilities, screenshotService, screenshotRegistry),
						cancellationToken))
			.WithDescription("Build a narrative of what happened during a date range.")
			.WithDetails("Best suited for day-scale retrospectives and timeline reconstruction.")
			.ReadOnly();
	}

	private static void MapSummaryPeriodCommand(IReplMap summary)
	{
		summary.Map(
			"period",
			(
				ReplDateRange period,
				IActivityRepository activityRepository,
				ITimelineRepository timelineRepository,
				IUsageRepository usageRepository,
				QueryCapabilityMatrix capabilities,
				IScreenshotService screenshotService,
				IScreenshotRegistry screenshotRegistry,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.BuildPeriodSummaryAsync(
						period,
						CreateNarrativeTools(activityRepository, timelineRepository, usageRepository, capabilities, screenshotService, screenshotRegistry),
						cancellationToken))
			.WithDescription("Build a multi-day summary with patterns and day breakdowns.")
			.ReadOnly();
	}

	private static void MapScreenshotCommands(ReplApp app)
	{
		app.Context("screenshot", screenshot =>
		{
			MapScreenshotListCommand(screenshot);
			MapScreenshotGetCommand(screenshot);
			MapScreenshotCropCommand(screenshot);
			MapScreenshotSaveCommand(screenshot);
		});
	}

	private static void MapScreenshotListCommand(IReplMap screenshot)
	{
		screenshot.Map(
				"list",
				(
					ReplDateTimeRange window,
					ScreenshotListOptions options,
					IScreenshotService screenshotService,
					CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.ListScreenshotsAsync(
						window,
						options,
						screenshotService,
						cancellationToken))
			.WithDescription("List screenshot metadata for a date-time window.")
			.WithDetails("Returns metadata only. Use screenshot get or screenshot crop to retrieve image bytes.")
			.ReadOnly();
	}

	private static void MapScreenshotGetCommand(IReplMap screenshot)
	{
		screenshot.Map(
				"get",
				(
					string screenshotRef,
					IScreenshotRegistry registry,
					IScreenshotService screenshotService) =>
					ManicTimeReplHandlers.GetScreenshot(
						screenshotRef,
						registry,
						screenshotService))
			.WithDescription("Fetch a screenshot payload by reference.")
			.WithDetails("Returns metadata plus thumbnail/full image payloads encoded as base64 text.")
			.ReadOnly();
	}

	private static void MapScreenshotCropCommand(IReplMap screenshot)
	{
		screenshot.Map(
			"crop",
				(
					string screenshotRef,
					double x,
					double y,
					double width,
					double height,
					string? coordinateUnits,
					IScreenshotRegistry registry,
					IScreenshotService screenshotService,
					ICropService cropService) =>
					ManicTimeReplHandlers.CropScreenshot(
						screenshotRef,
						x,
						y,
						width,
						height,
						coordinateUnits,
						registry,
						screenshotService,
						cropService))
			.WithDescription("Crop a screenshot region of interest.")
			.ReadOnly();
	}

	private static void MapScreenshotSaveCommand(IReplMap screenshot)
	{
		screenshot.Map(
			"save",
			(
				string screenshotRef,
				ScreenshotSaveOptions saveOptions,
				ScreenshotCropOptions cropOptions,
				IScreenshotRegistry registry,
				IScreenshotService screenshotService,
				ICropService cropService,
				IServiceProvider services,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.SaveScreenshotAsync(
						screenshotRef,
						saveOptions,
						cropOptions,
						registry,
						screenshotService,
						cropService,
						services,
						cancellationToken))
			.WithDescription("Persist a screenshot inside an MCP client root.")
			.WithDetails("Validates output paths against MCP client roots and can optionally save a cropped region.")
			.OpenWorld();
	}

	private static void MapResourceCommands(ReplApp app)
	{
		app.Context("resource", resource =>
		{
			MapResourceConfigCommand(resource);
			MapResourceTimelinesCommand(resource);
			MapResourceHealthCommand(resource);

			resource.Map("guide", ManicTimeReplHandlers.GetGuideResource)
				.WithDescription("Read the Repl-first usage guide.")
				.ReadOnly()
				.AsResource();

			MapResourceEnvironmentCommand(resource);
			MapResourceDataRangeCommand(resource);
		});
	}

	private static void MapResourceConfigCommand(IReplMap resource)
	{
		resource.Map(
			"config",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository,
				IScreenshotRegistry screenshotRegistry,
				IScreenshotService screenshotService) =>
					ManicTimeReplHandlers.GetConfigResource(
						CreateResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository,
							screenshotRegistry,
							screenshotService)))
			.WithDescription("Read the active ManicTime configuration.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapResourceTimelinesCommand(IReplMap resource)
	{
		resource.Map(
			"timelines",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository,
				IScreenshotRegistry screenshotRegistry,
				IScreenshotService screenshotService,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.GetTimelinesResourceAsync(
						CreateResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository,
							screenshotRegistry,
							screenshotService),
						cancellationToken))
			.WithDescription("Read the available timelines resource.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapResourceHealthCommand(IReplMap resource)
	{
		resource.Map(
			"health",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository,
				IScreenshotRegistry screenshotRegistry,
				IScreenshotService screenshotService) =>
					ManicTimeReplHandlers.GetHealthResource(
						CreateResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository,
							screenshotRegistry,
							screenshotService)))
			.WithDescription("Read the current health diagnostics resource.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapResourceEnvironmentCommand(IReplMap resource)
	{
		resource.Map(
			"environment",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository,
				IScreenshotRegistry screenshotRegistry,
				IScreenshotService screenshotService,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.GetEnvironmentResourceAsync(
						CreateResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository,
							screenshotRegistry,
							screenshotService),
						cancellationToken))
			.WithDescription("Read the device and runtime environment resource.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapResourceDataRangeCommand(IReplMap resource)
	{
		resource.Map(
			"data-range",
			(
				IDataDirectoryResolver resolver,
				IHealthService healthService,
				ITimelineRepository timelineRepository,
				IEnvironmentRepository environmentRepository,
				IUsageRepository usageRepository,
				IScreenshotRegistry screenshotRegistry,
				IScreenshotService screenshotService,
				CancellationToken cancellationToken) =>
					ManicTimeReplHandlers.GetDataRangeResourceAsync(
						CreateResources(
							resolver,
							healthService,
							timelineRepository,
							environmentRepository,
							usageRepository,
							screenshotRegistry,
							screenshotService),
						cancellationToken))
			.WithDescription("Read known data boundaries per timeline.")
			.ReadOnly()
			.AsResource();
	}

	private static void MapPromptCommands(ReplApp app)
	{
		app.Context("prompt", prompt =>
		{
			prompt.Map(
				"daily-review",
				(DateOnly date) => ManicTimeReplHandlers.BuildDailyReviewPrompt(date))
				.WithDescription("Guide a daily review workflow.")
				.AsPrompt();

			prompt.Map(
				"weekly-review",
				(ReplDateRange period) => ManicTimeReplHandlers.BuildWeeklyReviewPrompt(period))
				.WithDescription("Guide a multi-day review workflow.")
				.AsPrompt();

			prompt.Map(
				"screenshot-investigation",
				(ReplDateTimeRange window) => ManicTimeReplHandlers.BuildScreenshotInvestigationPrompt(window))
				.WithDescription("Guide a screenshot-led investigation workflow.")
				.AsPrompt();
		});
	}

	private static TimelineTools CreateTimelineTools(ITimelineRepository timelineRepository) =>
		new(timelineRepository);

	private static ActivityTools CreateActivityTools(
		IActivityRepository activityRepository,
		ITimelineRepository timelineRepository,
		IUsageRepository usageRepository,
		QueryCapabilityMatrix capabilities) =>
			new(activityRepository, timelineRepository, usageRepository, capabilities);

	private static NarrativeTools CreateNarrativeTools(
		IActivityRepository activityRepository,
		ITimelineRepository timelineRepository,
		IUsageRepository usageRepository,
		QueryCapabilityMatrix capabilities,
		IScreenshotService screenshotService,
		IScreenshotRegistry screenshotRegistry) =>
			new(activityRepository, timelineRepository, usageRepository, capabilities, screenshotService, screenshotRegistry);

	private static ManicTimeResources CreateResources(
		IDataDirectoryResolver resolver,
		IHealthService healthService,
		ITimelineRepository timelineRepository,
		IEnvironmentRepository environmentRepository,
		IUsageRepository usageRepository,
		IScreenshotRegistry screenshotRegistry,
		IScreenshotService screenshotService) =>
			new(resolver, healthService, timelineRepository, environmentRepository, usageRepository, screenshotRegistry, screenshotService);
}
