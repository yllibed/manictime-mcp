using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManicTimeMcp.Mcp;

/// <summary>Shared JSON serialization options for MCP responses.</summary>
internal static class JsonOptions
{
	/// <summary>Default options: camelCase, no indentation, compact output, null fields omitted.</summary>
	internal static JsonSerializerOptions Default { get; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
	};

	/// <summary>Same as <see cref="Default"/> but with indented output for human-readable CLI display.</summary>
	internal static JsonSerializerOptions Indented { get; } = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
		Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
	};
}
