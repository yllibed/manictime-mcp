using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddManicTimeConfiguration()
	.AddManicTimeDatabase()
	.AddManicTimeScreenshots()
	.AddMcpServer()
	.WithStdioServerTransport()
	.WithTools<TimelineTools>()
	.WithTools<ActivityTools>()
	.WithTools<ScreenshotTools>()
	.WithResources<ManicTimeResources>();

await builder.Build().RunAsync().ConfigureAwait(false);
