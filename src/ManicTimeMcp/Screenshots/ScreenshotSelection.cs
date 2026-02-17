namespace ManicTimeMcp.Screenshots;

/// <summary>Result of a screenshot selection query.</summary>
public sealed record ScreenshotSelection
{
	/// <summary>Selected screenshots matching the query criteria.</summary>
	public required IReadOnlyList<ScreenshotInfo> Screenshots { get; init; }

	/// <summary>Total number of screenshots matching the time window before sampling/limiting.</summary>
	public required int TotalMatching { get; init; }

	/// <summary>Whether the result was truncated by the max limit.</summary>
	public required bool IsTruncated { get; init; }

	/// <summary>Which sampling strategy was actually used for this selection.</summary>
	public SamplingStrategy SamplingStrategyUsed { get; init; } = SamplingStrategy.Interval;
}
