namespace ManicTimeMcp.Mcp.Models;

/// <summary>Response for the get_period_summary tool.</summary>
internal sealed class PeriodSummaryResponse
{
	/// <summary>Per-day breakdown.</summary>
	public required List<DaySummaryEntry> Days { get; init; }

	/// <summary>Aggregate stats across the period.</summary>
	public required PeriodAggregate Aggregate { get; init; }

	/// <summary>Day-of-week pattern analysis.</summary>
	public required PeriodPatterns Patterns { get; init; }

	/// <summary>Truncation info.</summary>
	public required TruncationInfo Truncation { get; init; }

	/// <summary>Diagnostics info.</summary>
	public required DiagnosticsInfo Diagnostics { get; init; }
}
