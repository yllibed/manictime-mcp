namespace ManicTimeMcp.Models;

/// <summary>Status of a single capability including fallback information.</summary>
public sealed record CapabilityStatus
{
	/// <summary>Capability name.</summary>
	public required string Name { get; init; }

	/// <summary>Whether the capability's underlying tables are fully available.</summary>
	public required bool Available { get; init; }

	/// <summary>Whether a fallback path is active for this capability.</summary>
	public bool FallbackActive { get; init; }
}
