namespace ManicTimeMcp.Mcp.Models;

/// <summary>Aggregate stats for a period.</summary>
internal sealed class PeriodAggregate
{
	/// <summary>Top apps.</summary>
	public required List<AppUsageEntry> TopApps { get; init; }

	/// <summary>Top websites.</summary>
	public List<WebUsageEntry>? TopWebsites { get; init; }

	/// <summary>Average daily minutes.</summary>
	public required double AvgDailyMinutes { get; init; }

	/// <summary>Busiest day.</summary>
	public string? BusiestDay { get; init; }

	/// <summary>Quietest day.</summary>
	public string? QuietestDay { get; init; }
}
