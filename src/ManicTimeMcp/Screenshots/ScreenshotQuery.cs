namespace ManicTimeMcp.Screenshots;

/// <summary>Parameters for selecting screenshots from the data directory.</summary>
public sealed record ScreenshotQuery
{
	/// <summary>Start of the time window (local time, inclusive).</summary>
	public required DateTime StartLocalTime { get; init; }

	/// <summary>End of the time window (local time, exclusive).</summary>
	public required DateTime EndLocalTime { get; init; }

	/// <summary>Minimum interval between selected screenshots. Null for no sampling.</summary>
	public TimeSpan? SamplingInterval { get; init; }

	/// <summary>Maximum number of screenshots to return. Null for default limit.</summary>
	public int? MaxCount { get; init; }

	/// <summary>Whether to prefer thumbnails over full-size images.</summary>
	public bool PreferThumbnails { get; init; } = true;

	/// <summary>Sampling strategy to use. Defaults to <see cref="SamplingStrategy.Interval"/>.</summary>
	public SamplingStrategy SamplingStrategy { get; init; } = SamplingStrategy.Interval;
}
