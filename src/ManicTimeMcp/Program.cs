using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

builder.Services
	.AddMcpServer()
	.WithStdioServerTransport();

await builder.Build().RunAsync().ConfigureAwait(false);
