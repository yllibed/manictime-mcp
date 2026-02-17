using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// MCP stdio transport uses stdout for JSON-RPC messages.
// Redirect all console log output to stderr so it doesn't corrupt the protocol.
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);

builder.Services
	.AddManicTimeConfiguration()
	.AddManicTimeDatabase()
	.AddManicTimeScreenshots()
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithTools<TimelineTools>()
	.WithTools<ActivityTools>()
	.WithTools<NarrativeTools>()
	.WithTools<ScreenshotToolsV2>()
	.WithResources<ManicTimeResources>()
	.WithPrompts<ManicTimePrompts>();

await builder.Build().RunAsync().ConfigureAwait(false);
