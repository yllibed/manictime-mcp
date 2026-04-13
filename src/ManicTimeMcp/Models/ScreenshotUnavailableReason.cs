using System.Text.Json.Serialization;
using ManicTimeMcp.Mcp;

namespace ManicTimeMcp.Models;

/// <summary>Likely reason screenshots are unavailable.</summary>
[JsonConverter(typeof(CamelCaseEnumConverter<ScreenshotUnavailableReason>))]
public enum ScreenshotUnavailableReason
{
	/// <summary>Screenshots are available; no reason applies.</summary>
	None,

	/// <summary>Retention policy may have removed screenshots.</summary>
	Retention,

	/// <summary>Screenshot capture may be disabled in ManicTime settings.</summary>
	CaptureDisabled,

	/// <summary>Reason could not be determined.</summary>
	Unknown,
}
