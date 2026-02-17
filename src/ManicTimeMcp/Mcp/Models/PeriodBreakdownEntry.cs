namespace ManicTimeMcp.Mcp.Models;

/// <summary>A single period entry in a website breakdown.</summary>
internal sealed class PeriodBreakdownEntry
{
	/// <summary>Period label (ISO-8601 date or datetime-hour).</summary>
	public required string Period { get; init; }

	/// <summary>Minutes of usage.</summary>
	public required double Minutes { get; init; }
}
