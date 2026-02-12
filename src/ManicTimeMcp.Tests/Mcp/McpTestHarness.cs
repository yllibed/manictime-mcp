using System.IO.Pipelines;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ManicTimeMcp.Tests.Mcp;

/// <summary>
/// In-process MCP server+client harness for integration tests.
/// Uses <see cref="Pipe"/> pairs to wire server and client without processes or sockets.
/// </summary>
internal sealed class McpTestHarness : IAsyncDisposable
{
	private readonly Pipe _clientToServer = new();
	private readonly Pipe _serverToClient = new();
	private readonly CancellationTokenSource _cts = new();
	private readonly ServiceProvider _serviceProvider;
	private readonly Task _serverTask;

	public McpTestHarness(Action<IServiceCollection, IMcpServerBuilder> configure)
	{
		var services = new ServiceCollection();
		services.AddLogging();

		var builder = services
			.AddMcpServer()
			.WithStreamServerTransport(
				_clientToServer.Reader.AsStream(),
				_serverToClient.Writer.AsStream());

		configure(services, builder);

		_serviceProvider = services.BuildServiceProvider();
		var server = _serviceProvider.GetRequiredService<McpServer>();
		_serverTask = server.RunAsync(_cts.Token);
	}

	public async Task<McpClient> CreateClientAsync(CancellationToken ct = default)
	{
		return await McpClient.CreateAsync(
			clientTransport: new StreamClientTransport(
				_clientToServer.Writer.AsStream(),
				_serverToClient.Reader.AsStream(),
				loggerFactory: null),
			clientOptions: null,
			loggerFactory: null,
			cancellationToken: ct).ConfigureAwait(false);
	}

	public async ValueTask DisposeAsync()
	{
		await _cts.CancelAsync().ConfigureAwait(false);
		await _clientToServer.Writer.CompleteAsync().ConfigureAwait(false);
		await _serverToClient.Writer.CompleteAsync().ConfigureAwait(false);

		try
		{
#pragma warning disable VSTHRD003 // Intentionally awaiting server task started in constructor for clean shutdown
			await _serverTask.ConfigureAwait(false);
#pragma warning restore VSTHRD003
		}
		catch (OperationCanceledException)
		{
			// Expected on shutdown
		}

		if (_serviceProvider is IAsyncDisposable ad)
		{
			await ad.DisposeAsync().ConfigureAwait(false);
		}

		_cts.Dispose();
	}
}
