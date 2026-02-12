using System.Diagnostics;
using System.Text;
using AwesomeAssertions;

namespace ManicTimeMcp.Tests.Mcp;

/// <summary>
/// Process-level integration tests verifying that the MCP server
/// keeps stdout clean for JSON-RPC and sends all log output to stderr.
/// </summary>
[TestClass]
public sealed class StdioTransportTests
{
	/// <summary>
	/// Resolves the ManicTimeMcp.exe path from the test output directory.
	/// The exe is copied here because the test project has a ProjectReference.
	/// </summary>
	private static string GetServerExePath()
	{
		var testDir = AppContext.BaseDirectory;
		var exePath = Path.Combine(testDir, "ManicTimeMcp.exe");
		if (!File.Exists(exePath))
		{
			Assert.Inconclusive($"ManicTimeMcp.exe not found at {exePath}");
		}

		return exePath;
	}

	[TestMethod]
	public async Task Stdout_ContainsNoLogOutput_WhenServerStarts()
	{
		var exePath = GetServerExePath();

		using var process = new Process();
		process.StartInfo = new ProcessStartInfo
		{
			FileName = exePath,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		process.Start();

		// Close stdin immediately to trigger a clean shutdown.
		process.StandardInput.Close();

		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

		var stdoutTask = process.StandardOutput.ReadToEndAsync(cts.Token);
		var stderrTask = process.StandardError.ReadToEndAsync(cts.Token);

		await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

		var stdout = await stdoutTask.ConfigureAwait(false);
		var stderr = await stderrTask.ConfigureAwait(false);

		// Stdout must be clean — no log lines that would corrupt the MCP protocol.
		stdout.Should().BeEmpty("stdout must be reserved for JSON-RPC messages");

		// Stderr should contain the expected log output.
		stderr.Should().Contain("ManicTimeMcp", "log output should go to stderr");
	}

	[TestMethod]
	public async Task Stdout_ReturnsInitializeResponse_WhenHandshakeIsSent()
	{
		var exePath = GetServerExePath();

		using var process = new Process();
		process.StartInfo = new ProcessStartInfo
		{
			FileName = exePath,
			RedirectStandardInput = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			UseShellExecute = false,
			CreateNoWindow = true,
		};

		process.Start();

		// Send an MCP initialize request (newline-delimited JSON-RPC).
		const string initializeRequest = """
			{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"test","version":"1.0"}}}
			""";

		await process.StandardInput.WriteLineAsync(initializeRequest.Trim()).ConfigureAwait(false);
		await process.StandardInput.FlushAsync().ConfigureAwait(false);

		// Read the response (first line of stdout).
		using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
		var responseLine = await ReadLineWithTimeoutAsync(process.StandardOutput, cts.Token).ConfigureAwait(false);

		// Close stdin to trigger shutdown.
		process.StandardInput.Close();
		await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);

		// The response must be valid JSON-RPC.
		responseLine.Should().NotBeNullOrEmpty("server should return an initialize response");
		responseLine.Should().StartWith("{", "response must be JSON");
		responseLine.Should().Contain("\"jsonrpc\"", "response must be JSON-RPC");
		responseLine.Should().Contain("\"protocolVersion\"", "response must include protocol version");

		// It must NOT contain log text.
		responseLine.Should().NotContain("info:", "stdout must not contain log output");
	}

	private static async Task<string?> ReadLineWithTimeoutAsync(StreamReader reader, CancellationToken ct)
	{
		var lineTask = reader.ReadLineAsync(ct).AsTask();
		var completed = await Task.WhenAny(lineTask, Task.Delay(Timeout.Infinite, ct)).ConfigureAwait(false);
		return completed == lineTask ? await lineTask.ConfigureAwait(false) : null;
	}
}
