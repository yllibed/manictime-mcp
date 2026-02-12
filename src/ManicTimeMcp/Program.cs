using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddManicTimeConfiguration()
	.AddManicTimeDatabase()
	.AddMcpServer()
	.WithStdioServerTransport();

await builder.Build().RunAsync().ConfigureAwait(false);
