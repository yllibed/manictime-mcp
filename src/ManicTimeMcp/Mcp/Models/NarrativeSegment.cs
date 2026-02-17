namespace ManicTimeMcp.Mcp.Models;

/// <summary>A single activity segment within a narrative.</summary>
internal sealed class NarrativeSegment
{
	/// <summary>Segment start time (ISO-8601).</summary>
	public required string Start { get; init; }

	/// <summary>Segment end time (ISO-8601).</summary>
	public required string End { get; init; }

	/// <summary>Duration in minutes.</summary>
	public required double DurationMinutes { get; init; }

	/// <summary>Application name.</summary>
	public string? Application { get; init; }

	/// <summary>Application color.</summary>
	public string? ApplicationColor { get; init; }

	/// <summary>Document name.</summary>
	public string? Document { get; init; }

	/// <summary>Active website during this segment (from Documents timeline, GroupType=WebSites).</summary>
	public string? Website { get; init; }

	/// <summary>Tags associated with this activity segment, or null if tag data unavailable.</summary>
	public string[]? Tags { get; init; }

	/// <summary>Opaque references for tool chaining, or null.</summary>
	public SegmentRefs? Refs { get; init; }
}
