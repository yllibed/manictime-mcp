using System.Text.Json.Serialization;
using ManicTimeMcp.Mcp;

namespace ManicTimeMcp.Models;

/// <summary>Overall health status of the ManicTime MCP server environment.</summary>
[JsonConverter(typeof(CamelCaseEnumConverter<HealthStatus>))]
public enum HealthStatus
{
	/// <summary>All checks passed; server is fully operational.</summary>
	Healthy,

	/// <summary>Informational issues exist; functionality is believed intact but not verified for this configuration.</summary>
	PotentiallyDegraded,

	/// <summary>Non-fatal warnings exist; server is operational with reduced functionality.</summary>
	Degraded,

	/// <summary>Fatal issues exist; server cannot operate normally.</summary>
	Unhealthy,
}
