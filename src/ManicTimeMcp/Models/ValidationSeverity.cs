namespace ManicTimeMcp.Models;

/// <summary>Severity classification for health and installation issues.</summary>
public enum ValidationSeverity
{
	/// <summary>Fatal issue that prevents normal operation.</summary>
	Fatal,

	/// <summary>Non-fatal condition that may limit functionality.</summary>
	Warning,
}
