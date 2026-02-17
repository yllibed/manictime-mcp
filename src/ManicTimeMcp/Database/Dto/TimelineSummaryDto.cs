namespace ManicTimeMcp.Database.Dto;

/// <summary>Timeline summary from Ar_TimelineSummary.</summary>
public sealed record TimelineSummaryDto
{
	/// <summary>Timeline/report identifier.</summary>
	public required long ReportId { get; init; }

	/// <summary>Start of data range in local time.</summary>
	public required string StartLocalTime { get; init; }

	/// <summary>End of data range in local time.</summary>
	public required string EndLocalTime { get; init; }
}
