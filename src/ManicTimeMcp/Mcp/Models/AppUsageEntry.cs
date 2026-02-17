namespace ManicTimeMcp.Mcp.Models;

/// <summary>Application usage entry for narratives and summaries.</summary>
internal sealed class AppUsageEntry
{
	/// <summary>Application name.</summary>
	public required string Name { get; init; }

	/// <summary>Application color.</summary>
	public string? Color { get; init; }

	/// <summary>Total minutes.</summary>
	public required double TotalMinutes { get; init; }
}
