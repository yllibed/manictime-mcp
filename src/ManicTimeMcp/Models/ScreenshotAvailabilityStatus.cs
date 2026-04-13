using System.Text.Json.Serialization;
using ManicTimeMcp.Mcp;

namespace ManicTimeMcp.Models;

/// <summary>Availability status of the screenshot directory.</summary>
[JsonConverter(typeof(CamelCaseEnumConverter<ScreenshotAvailabilityStatus>))]
public enum ScreenshotAvailabilityStatus
{
	/// <summary>Screenshots are present and accessible.</summary>
	Available,

	/// <summary>Screenshots are not available.</summary>
	Unavailable,

	/// <summary>Status could not be determined (for example, data directory unresolved).</summary>
	Unknown,
}
