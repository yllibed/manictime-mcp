namespace ManicTimeMcp.Mcp.Models;

/// <summary>A curated screenshot suggestion for narrative reports.</summary>
internal sealed class SuggestedScreenshot
{
	/// <summary>Screenshot reference for use with get_screenshot.</summary>
	public required string ScreenshotRef { get; init; }

	/// <summary>Timestamp of the segment (local time, yyyy-MM-dd HH:mm:ss).</summary>
	public required string Timestamp { get; init; }

	/// <summary>Application active at this time.</summary>
	public string? Application { get; init; }

	/// <summary>Why this screenshot was suggested (e.g. "app transition", "long session").</summary>
	public string? Hint { get; init; }
}
