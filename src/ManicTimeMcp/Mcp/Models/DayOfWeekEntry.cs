namespace ManicTimeMcp.Mcp.Models;

/// <summary>Day-of-week usage entry.</summary>
internal sealed class DayOfWeekEntry
{
	/// <summary>Day of week (0=Sunday..6=Saturday).</summary>
	public required int DayOfWeek { get; init; }

	/// <summary>Total minutes.</summary>
	public required double TotalMinutes { get; init; }
}
