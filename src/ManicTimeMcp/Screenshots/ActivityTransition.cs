namespace ManicTimeMcp.Screenshots;

/// <summary>Represents a transition between activities at a specific timestamp.</summary>
public sealed record ActivityTransition
{
	/// <summary>Timestamp when the transition occurred (local time).</summary>
	public required DateTime Timestamp { get; init; }

	/// <summary>Activity name before the transition, or null if starting.</summary>
	public string? FromActivity { get; init; }

	/// <summary>Activity name after the transition.</summary>
	public required string ToActivity { get; init; }
}
