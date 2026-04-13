using System.Text.Json;
using System.Text.Json.Serialization;

namespace ManicTimeMcp.Mcp;

/// <summary>Serializes enums as camelCase strings. Use as <c>[JsonConverter(typeof(CamelCaseEnumConverter&lt;T&gt;))]</c>.</summary>
internal sealed class CamelCaseEnumConverter<T> : JsonStringEnumConverter<T>
	where T : struct, Enum
{
	/// <summary>Creates a converter that uses <see cref="JsonNamingPolicy.CamelCase"/>.</summary>
	public CamelCaseEnumConverter() : base(JsonNamingPolicy.CamelCase)
	{
	}
}
