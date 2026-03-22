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
		app.MapModule(new TimelineModule());
		app.MapModule(new ActivityModule());
		app.MapModule(new UsageModule());
		app.MapModule(new SummaryModule());
		app.MapModule(new ScreenshotModule());
		app.MapModule(new ResourceModule());
		app.MapModule(new PromptModule());

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
}
