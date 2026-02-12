namespace ManicTimeMcp.Models;

/// <summary>Likely reason screenshots are unavailable.</summary>
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
