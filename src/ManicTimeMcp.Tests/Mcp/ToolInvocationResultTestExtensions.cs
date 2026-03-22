using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Mcp;

namespace ManicTimeMcp.Tests.Mcp;

internal static class ToolInvocationResultTestExtensions
{
	public static JsonDocument ParsePayload(this ToolInvocationResult result)
	{
		result.IsError.Should().BeFalse($"expected a successful payload but got: {result.Payload}");
		return JsonDocument.Parse(result.Payload);
	}
}
