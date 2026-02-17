namespace ManicTimeMcp.Database.Dto;

/// <summary>
/// Read model for a ManicTime activity enriched with group, common group, and tag data.
/// When supplemental tables are available, includes resolved names, colors, and tags.
/// </summary>
public sealed record EnrichedActivityDto
{
	/// <summary>Activity identifier (Ar_Activity.ActivityId).</summary>
	public required long ActivityId { get; init; }

	/// <summary>Report/timeline this activity belongs to (Ar_Activity.ReportId).</summary>
	public required long ReportId { get; init; }

	/// <summary>Activity start time in local time.</summary>
	public required string StartLocalTime { get; init; }

	/// <summary>Activity end time in local time.</summary>
	public required string EndLocalTime { get; init; }

	/// <summary>Name of the activity (e.g. process name, document title).</summary>
	public required string? Name { get; init; }

	/// <summary>Group identifier for hierarchical grouping, or null.</summary>
	public required long? GroupId { get; init; }

	/// <summary>Group display name from Ar_Group, or null if no group.</summary>
	public string? GroupName { get; init; }

	/// <summary>Group color from Ar_Group, or null.</summary>
	public string? GroupColor { get; init; }

	/// <summary>Group key from Ar_Group, or null.</summary>
	public string? GroupKey { get; init; }

	/// <summary>Common group display name from Ar_CommonGroup, or null if unavailable.</summary>
	public string? CommonGroupName { get; init; }

	/// <summary>Tags associated with this activity, or null if tag tables unavailable.</summary>
	public string[]? Tags { get; init; }
}
