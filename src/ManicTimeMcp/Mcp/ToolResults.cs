using System.Text.Json.Nodes;
using Repl;

namespace ManicTimeMcp.Mcp;

/// <summary>Factory and adapter methods for transport-neutral tool responses.</summary>
internal static class ToolResults
{
	/// <summary>Creates a successful tool result containing a JSON payload.</summary>
	internal static ToolInvocationResult Success(string json) =>
		new(isError: false, payload: json);

	/// <summary>Creates an error tool result.</summary>
	internal static ToolInvocationResult Error(string message, string? errorCode = null) =>
		new(isError: true, payload: message, errorCode: errorCode);

	/// <summary>Converts a transport-neutral tool result into a Repl return payload.</summary>
	internal static object ToReplResult(ToolInvocationResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		if (result.IsError)
		{
			return string.IsNullOrWhiteSpace(result.ErrorCode)
				? Results.Error("manictime_error", result.Payload)
				: Results.Error(result.ErrorCode!, result.Payload);
		}

		return TryParseJson(result.Payload, out var node)
			? node!
			: result.Payload;
	}

	private static bool TryParseJson(string text, out JsonNode? node)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			node = null;
			return false;
		}

		try
		{
			node = JsonNode.Parse(text);
			return node is not null;
		}
		catch (System.Text.Json.JsonException)
		{
			node = null;
			return false;
		}
	}
}
