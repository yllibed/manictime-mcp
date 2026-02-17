namespace ManicTimeMcp.Mcp.Models;

/// <summary>Opaque references for a narrative segment, used for tool chaining.</summary>
internal sealed class SegmentRefs
{
	/// <summary>Timeline/report identifier.</summary>
	public required long TimelineRef { get; init; }

	/// <summary>Activity identifier.</summary>
	public required long ActivityRef { get; init; }
}
