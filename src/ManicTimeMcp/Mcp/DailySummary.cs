namespace ManicTimeMcp.Mcp;

/// <summary>Compact daily summary result for MCP output.</summary>
internal sealed class DailySummary
{
	/// <summary>Date being summarized.</summary>
	public required string Date { get; init; }

	/// <summary>Summary entries per timeline.</summary>
	public List<TimelineSummaryEntry> TimelineSummaries { get; } = [];
}
