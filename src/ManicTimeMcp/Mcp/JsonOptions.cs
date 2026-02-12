using System.Text.Json;

namespace ManicTimeMcp.Mcp;

/// <summary>Shared JSON serialization options for MCP responses.</summary>
internal static class JsonOptions
{
	/// <summary>Default options: camelCase, no indentation, compact output.</summary>
	internal static JsonSerializerOptions Default { get; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
	};
}
