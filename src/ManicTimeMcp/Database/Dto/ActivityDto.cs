namespace ManicTimeMcp.Database.Dto;

/// <summary>Read model for a ManicTime activity row.</summary>
public sealed record ActivityDto
{
	/// <summary>Activity identifier (Ar_Activity.ActivityId).</summary>
	public required long ActivityId { get; init; }

	/// <summary>Timeline this activity belongs to (Ar_Activity.TimelineId).</summary>
	public required long TimelineId { get; init; }

	/// <summary>Activity start time in local time.</summary>
	public required string StartLocalTime { get; init; }

	/// <summary>Activity end time in local time.</summary>
	public required string EndLocalTime { get; init; }

	/// <summary>Display name of the activity (e.g. application name, document title).</summary>
	public required string? DisplayName { get; init; }

	/// <summary>Group identifier for hierarchical grouping, or null.</summary>
	public required long? GroupId { get; init; }
}
