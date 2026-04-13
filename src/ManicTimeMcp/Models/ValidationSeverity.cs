using System.Text.Json.Serialization;
using ManicTimeMcp.Mcp;

namespace ManicTimeMcp.Models;

/// <summary>Severity classification for health and installation issues.</summary>
[JsonConverter(typeof(CamelCaseEnumConverter<ValidationSeverity>))]
public enum ValidationSeverity
{
	/// <summary>Fatal issue that prevents normal operation.</summary>
	Fatal,

	/// <summary>Non-fatal condition that reduces functionality.</summary>
	Warning,

	/// <summary>Informational condition; functionality is believed intact but not verified for this configuration.</summary>
	Info,
}
