namespace ManicTimeMcp.Database.Dto;

/// <summary>Read model for a ManicTime group row.</summary>
public sealed record GroupDto
{
	/// <summary>Group identifier (Ar_Group.GroupId).</summary>
	public required long GroupId { get; init; }

	/// <summary>Timeline this group belongs to (Ar_Group.TimelineId).</summary>
	public required long TimelineId { get; init; }

	/// <summary>Display name of the group.</summary>
	public required string DisplayName { get; init; }

	/// <summary>Parent group identifier for hierarchical nesting, or null for root groups.</summary>
	public required long? ParentGroupId { get; init; }
}
