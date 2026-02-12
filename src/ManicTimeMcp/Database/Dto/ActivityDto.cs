namespace ManicTimeMcp.Database.Dto;

/// <summary>Read model for a ManicTime activity row.</summary>
public sealed record ActivityDto
{
	/// <summary>Activity identifier (Ar_Activity.ActivityId).</summary>
	public required long ActivityId { get; init; }

	/// <summary>Report/timeline this activity belongs to (Ar_Activity.ReportId).</summary>
	public required long ReportId { get; init; }

	/// <summary>Activity start time in local time.</summary>
	public required string StartLocalTime { get; init; }

	/// <summary>Activity end time in local time.</summary>
	public required string EndLocalTime { get; init; }

	/// <summary>Name of the activity (e.g. application name, document title).</summary>
	public required string? Name { get; init; }

	/// <summary>Group identifier for hierarchical grouping, or null.</summary>
	public required long? GroupId { get; init; }
}
