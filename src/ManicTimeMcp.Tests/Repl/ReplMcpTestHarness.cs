using System.IO.Pipelines;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using Repl;

namespace ManicTimeMcp.Tests.Repl;

/// <summary>In-process MCP harness backed by the Repl command graph.</summary>
internal sealed class ReplMcpTestHarness : IAsyncDisposable
{
	private readonly CancellationTokenSource _cts;
	private readonly Pipe _clientToServer;
	private readonly Pipe _serverToClient;
	private readonly Task _serverTask;
	private readonly McpClient _client;
	private readonly McpServer _server;

	private ReplMcpTestHarness(
		McpServer server,
		McpClient client,
		Task serverTask,
		Pipe clientToServer,
		Pipe serverToClient,
		CancellationTokenSource cts)
	{
		_server = server;
		_client = client;
		_serverTask = serverTask;
		_clientToServer = clientToServer;
		_serverToClient = serverToClient;
		_cts = cts;
	}

	public McpClient Client => _client;

	public static Task<ReplMcpTestHarness> CreateAsync(ReplApp app)
	{
		ArgumentNullException.ThrowIfNull(app);
		return CreateCoreAsync(app);
	}

	public static async Task<ReplMcpTestHarness> CreateAsync(Func<ReplApp> appFactory)
	{
		ArgumentNullException.ThrowIfNull(appFactory);

		var app = appFactory();
		return await CreateCoreAsync(app).ConfigureAwait(false);
	}

	private static async Task<ReplMcpTestHarness> CreateCoreAsync(ReplApp app)
	{
		var options = ManicTimeMcp.Repl.ManicTimeReplApp.BuildMcpServerOptions(app);
		var services = app.Services;
		var serverName = options.ServerInfo?.Name ?? "ManicTime MCP";

		var clientToServer = new Pipe();
		var serverToClient = new Pipe();
		var transport = new StreamServerTransport(
			clientToServer.Reader.AsStream(),
			serverToClient.Writer.AsStream(),
			serverName);
		var cts = new CancellationTokenSource();
		var server = McpServer.Create(transport, options, serviceProvider: services);
		var serverTask = server.RunAsync(cts.Token);

		var client = await McpClient.CreateAsync(
			new StreamClientTransport(
				clientToServer.Writer.AsStream(),
				serverToClient.Reader.AsStream())).ConfigureAwait(false);

		return new ReplMcpTestHarness(server, client, serverTask, clientToServer, serverToClient, cts);
	}

	public async ValueTask DisposeAsync()
	{
		await _client.DisposeAsync().ConfigureAwait(false);
		await _cts.CancelAsync().ConfigureAwait(false);
		await _clientToServer.Writer.CompleteAsync().ConfigureAwait(false);
		await _serverToClient.Writer.CompleteAsync().ConfigureAwait(false);

		try
		{
			await _serverTask.WaitAsync(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
		}
		catch (OperationCanceledException)
		{
		}
		catch (TimeoutException)
		{
		}

		await _server.DisposeAsync().ConfigureAwait(false);
		_cts.Dispose();
	}
}
