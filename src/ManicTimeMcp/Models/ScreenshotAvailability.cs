namespace ManicTimeMcp.Models;

/// <summary>Screenshot directory health sub-report.</summary>
public sealed record ScreenshotAvailability
{
	/// <summary>Current availability status of the screenshot directory.</summary>
	public required ScreenshotAvailabilityStatus Status { get; init; }

	/// <summary>Likely reason when screenshots are unavailable.</summary>
	public required ScreenshotUnavailableReason Reason { get; init; }

	/// <summary>Remediation hint when screenshots are unavailable.</summary>
	public string? RemediationHint { get; init; }
}
