using System.Text.Json;
using System.Text.Json.Nodes;
using ManicTimeMcp.Mcp;
using ModelContextProtocol.Protocol;
using Repl;

namespace ManicTimeMcp.Repl;

/// <summary>Adapts legacy MCP tool payloads to Repl-friendly results during the migration.</summary>
internal static class ReplToolResultAdapter
{
	/// <summary>Converts a legacy <see cref="CallToolResult"/> to a Repl return payload.</summary>
	public static object FromCallToolResult(CallToolResult result)
	{
		ArgumentNullException.ThrowIfNull(result);

		var text = result.Content
			.OfType<TextContentBlock>()
			.Select(block => block.Text)
			.FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))
			?? string.Empty;

		if (result.IsError is true)
		{
			return Results.Error("manictime_error", text);
		}

		if (TryParseJson(text, out var node))
		{
			return node!;
		}

		return text;
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
		catch (JsonException)
		{
			node = null;
			return false;
		}
	}
}
