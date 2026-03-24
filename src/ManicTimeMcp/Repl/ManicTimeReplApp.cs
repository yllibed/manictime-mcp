using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;
using Repl;
using Repl.Mcp;

namespace ManicTimeMcp.Repl;

/// <summary>Creates the Repl-first ManicTime command surface and MCP integration.</summary>
public static class ManicTimeReplApp
{
	/// <summary>Creates the configured Repl application.</summary>
	public static ReplApp Create(Action<IServiceCollection>? configureServices = null)
	{
		var app = ReplApp.Create(services => ConfigureServices(services, configureServices)).UseDefaultInteractive();

		app.UseMcpServer(ConfigureMcpOptions);
		app.MapModule<TimelineModule>();
		app.MapModule<ActivityModule>();
		app.MapModule<UsageModule>();
		app.MapModule<SummaryModule>();
		app.MapModule<ScreenshotModule>();
		app.MapModule<WorkspaceModule>();
		app.MapModule<ResourceModule>();
		app.MapModule<PromptModule>();

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
		return app.BuildMcpServerOptions(ConfigureMcpOptions);
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

		services.AddSingleton<TimelineTools>();
		services.AddSingleton<ActivityTools>();
		services.AddSingleton<NarrativeTools>();
		services.AddSingleton<ManicTimeResources>();
		services.AddSingleton<IMcpClientRoots, NullMcpClientRoots>();

		services.AddSingleton<TimelineModule>();
		services.AddSingleton<ActivityModule>();
		services.AddSingleton<UsageModule>();
		services.AddSingleton<SummaryModule>();
		services.AddSingleton<ScreenshotModule>();
		services.AddSingleton<WorkspaceModule>();
		services.AddSingleton<ResourceModule>();
		services.AddSingleton<PromptModule>();

		configureServices?.Invoke(services);
	}

	private static void ConfigureMcpOptions(ReplMcpServerOptions options)
	{
		options.ServerName = "ManicTime MCP";
		options.ServerVersion = HealthService.GetServerVersion();
		options.ResourceUriScheme = "manictime";
		options.AutoPromoteReadOnlyToResources = false;
	}
}
