namespace ManicTimeMcp.Database.Dto;

/// <summary>Read model for a ManicTime group row.</summary>
public sealed record GroupDto
{
	/// <summary>Group identifier (Ar_Group.GroupId).</summary>
	public required long GroupId { get; init; }

	/// <summary>Report/timeline this group belongs to (Ar_Group.ReportId).</summary>
	public required long ReportId { get; init; }

	/// <summary>Name of the group.</summary>
	public required string Name { get; init; }
}
