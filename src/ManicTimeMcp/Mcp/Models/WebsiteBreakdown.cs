namespace ManicTimeMcp.Mcp.Models;

/// <summary>Website with time breakdown.</summary>
internal sealed class WebsiteBreakdown
{
	/// <summary>Website name.</summary>
	public required string Name { get; init; }

	/// <summary>Total minutes.</summary>
	public required double TotalMinutes { get; init; }

	/// <summary>Breakdown by period.</summary>
	public required List<PeriodBreakdownEntry> TimeBreakdown { get; init; }
}
