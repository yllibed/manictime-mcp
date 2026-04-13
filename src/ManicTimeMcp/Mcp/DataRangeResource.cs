using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Mcp;

/// <summary>Resource payload for data range information.</summary>
public sealed record DataRangeResource
{
	/// <summary>Timeline summary entries with date boundaries.</summary>
	public required IReadOnlyList<TimelineSummaryDto> TimelineSummaries { get; init; }
}
