namespace ManicTimeMcp.Screenshots;

/// <summary>Parsed metadata from a ManicTime screenshot filename.</summary>
public sealed record ScreenshotInfo
{
	/// <summary>Date portion of the timestamp (yyyy-MM-dd).</summary>
	public required DateOnly Date { get; init; }

	/// <summary>Time portion of the timestamp (HH:mm:ss).</summary>
	public required TimeOnly Time { get; init; }

	/// <summary>UTC offset string (e.g. "+02-00").</summary>
	public required string Offset { get; init; }

	/// <summary>Screenshot width in pixels.</summary>
	public required int Width { get; init; }

	/// <summary>Screenshot height in pixels.</summary>
	public required int Height { get; init; }

	/// <summary>Sequence number within the same timestamp.</summary>
	public required int Sequence { get; init; }

	/// <summary>Monitor index.</summary>
	public required int Monitor { get; init; }

	/// <summary>Whether this is a thumbnail variant.</summary>
	public required bool IsThumbnail { get; init; }

	/// <summary>Full file path.</summary>
	public required string FilePath { get; init; }

	/// <summary>
	/// Opaque reference string assigned after registration with <see cref="IScreenshotRegistry"/>.
	/// Null until the screenshot is registered.
	/// </summary>
	public string? Ref { get; set; }

	/// <summary>Combined local timestamp derived from date and time components.</summary>
	public DateTime LocalTimestamp => Date.ToDateTime(Time);
}
