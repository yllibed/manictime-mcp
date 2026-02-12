namespace ManicTimeMcp.Models;

/// <summary>Overall health status of the ManicTime MCP server environment.</summary>
public enum HealthStatus
{
	/// <summary>All checks passed; server is fully operational.</summary>
	Healthy,

	/// <summary>Non-fatal warnings exist; server is operational with reduced functionality.</summary>
	Degraded,

	/// <summary>Fatal issues exist; server cannot operate normally.</summary>
	Unhealthy,
}
