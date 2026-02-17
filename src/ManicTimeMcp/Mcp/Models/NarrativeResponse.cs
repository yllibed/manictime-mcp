namespace ManicTimeMcp.Mcp.Models;

/// <summary>Response for the get_activity_narrative tool.</summary>
internal sealed class NarrativeResponse
{
	/// <summary>Start date.</summary>
	public required string StartDate { get; init; }

	/// <summary>End date.</summary>
	public required string EndDate { get; init; }

	/// <summary>Total active minutes.</summary>
	public double TotalActiveMinutes { get; init; }

	/// <summary>Activity segments.</summary>
	public required List<NarrativeSegment> Segments { get; init; }

	/// <summary>Top applications by usage.</summary>
	public required List<AppUsageEntry> TopApplications { get; init; }

	/// <summary>Top websites by usage.</summary>
	public List<WebUsageEntry>? TopWebsites { get; init; }

	/// <summary>Curated screenshots suggested for inclusion in reports, or null if none available.</summary>
	public List<SuggestedScreenshot>? SuggestedScreenshots { get; init; }

	/// <summary>Hourly website breakdown, or null if not requested.</summary>
	public List<WebsiteBreakdown>? HourlyWebBreakdown { get; init; }

	/// <summary>Truncation info.</summary>
	public required TruncationInfo Truncation { get; init; }

	/// <summary>Diagnostics info.</summary>
	public required DiagnosticsInfo Diagnostics { get; init; }
}
