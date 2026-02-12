using ModelContextProtocol.Protocol;

namespace ManicTimeMcp.Mcp;

/// <summary>Factory methods for creating <see cref="CallToolResult"/> responses.</summary>
internal static class ToolResults
{
	/// <summary>Creates a successful tool result containing a JSON payload.</summary>
	internal static CallToolResult Success(string json) =>
		new() { Content = [new TextContentBlock { Text = json }] };

	/// <summary>Creates an error tool result with <see cref="CallToolResult.IsError"/> set.</summary>
	internal static CallToolResult Error(string message) =>
		new() { IsError = true, Content = [new TextContentBlock { Text = message }] };
}
