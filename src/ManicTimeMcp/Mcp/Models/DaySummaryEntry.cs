namespace ManicTimeMcp.Mcp.Models;

/// <summary>Summary for a single day.</summary>
internal sealed class DaySummaryEntry
{
	/// <summary>Date (ISO-8601).</summary>
	public required string Date { get; init; }

	/// <summary>Total active minutes.</summary>
	public required double TotalActiveMinutes { get; init; }

	/// <summary>Top application.</summary>
	public string? TopApp { get; init; }

	/// <summary>First activity time.</summary>
	public string? FirstActivity { get; init; }

	/// <summary>Last activity time.</summary>
	public string? LastActivity { get; init; }
}
