using ManicTimeMcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddManicTimeConfiguration()
	.AddMcpServer()
	.WithStdioServerTransport();

await builder.Build().RunAsync().ConfigureAwait(false);
