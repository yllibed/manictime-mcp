namespace ManicTimeMcp.Database.Dto;

/// <summary>Read model for a cross-timeline correlated activity.</summary>
public sealed record CorrelatedActivityDto
{
	/// <summary>Activity start time in local time.</summary>
	public required string StartLocalTime { get; init; }

	/// <summary>Activity end time in local time.</summary>
	public required string EndLocalTime { get; init; }

	/// <summary>Activity name (e.g. process name, document title).</summary>
	public required string? Name { get; init; }

	/// <summary>Timeline schema name (e.g. ManicTime/Applications, ManicTime/Documents).</summary>
	public required string SchemaName { get; init; }

	/// <summary>Group display name from Ar_Group, or null if no group.</summary>
	public string? GroupName { get; init; }

	/// <summary>Group color from Ar_Group, or null.</summary>
	public string? GroupColor { get; init; }

	/// <summary>Common group display name from Ar_CommonGroup, or null if unavailable.</summary>
	public string? CommonGroupName { get; init; }
}
