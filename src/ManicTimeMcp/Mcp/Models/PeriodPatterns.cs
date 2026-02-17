namespace ManicTimeMcp.Mcp.Models;

/// <summary>Day-of-week patterns.</summary>
internal sealed class PeriodPatterns
{
	/// <summary>Usage distribution by day of week.</summary>
	public required List<DayOfWeekEntry> DayOfWeekDistribution { get; init; }
}
