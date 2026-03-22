using System.Text.Json;
using AwesomeAssertions;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class ServerManifestTests
{
	[TestMethod]
	public void ServerManifest_DeclaresExplicitMcpServeArguments()
	{
		var manifestPath = Path.GetFullPath(Path.Combine(
			AppContext.BaseDirectory,
			"..", "..", "..", "..",
			"ManicTimeMcp",
			".mcp",
			"server.json"));

		File.Exists(manifestPath).Should().BeTrue($"manifest should exist at {manifestPath}");

		using var document = JsonDocument.Parse(File.ReadAllText(manifestPath));
		var package = document.RootElement.GetProperty("packages")[0];

		package.GetProperty("runtimeHint").GetString().Should().Be("dnx");
		package.GetProperty("runtimeArguments").EnumerateArray()
			.Select(static argument => $"{argument.GetProperty("type").GetString()}:{argument.GetProperty("value").GetString()}")
			.Should().ContainSingle(argument => string.Equals(argument, "positional:-y", StringComparison.Ordinal));
		package.GetProperty("packageArguments").EnumerateArray()
			.Select(static argument => $"{argument.GetProperty("type").GetString()}:{argument.GetProperty("value").GetString()}")
			.Should().ContainInOrder("positional:mcp", "positional:serve");
	}
}
