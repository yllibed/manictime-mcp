using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddManicTimeConfiguration()
	.AddManicTimeDatabase()
	.AddManicTimeScreenshots()
	.AddMcpServer()
	.WithStdioServerTransport();

await builder.Build().RunAsync().ConfigureAwait(false);
