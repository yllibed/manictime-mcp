namespace ManicTimeMcp.Mcp;

/// <summary>Summary of a single timeline's activity count for a day.</summary>
internal sealed class TimelineSummaryEntry
{
	/// <summary>Timeline identifier.</summary>
	public required long TimelineId { get; init; }

	/// <summary>Schema name of the timeline.</summary>
	public required string SchemaName { get; init; }

	/// <summary>Number of activities in this timeline for the day.</summary>
	public required int ActivityCount { get; init; }
}
