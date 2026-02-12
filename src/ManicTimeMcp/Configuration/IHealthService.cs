using ManicTimeMcp.Models;

namespace ManicTimeMcp.Configuration;

/// <summary>Builds the health diagnostic report for the ManicTime MCP environment.</summary>
public interface IHealthService
{
	/// <summary>Generates a complete health report with current environment status.</summary>
	HealthReport GetHealthReport();
}
