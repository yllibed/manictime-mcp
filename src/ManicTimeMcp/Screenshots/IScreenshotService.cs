namespace ManicTimeMcp.Screenshots;

/// <summary>Provides screenshot selection and secure file reading.</summary>
public interface IScreenshotService
{
	/// <summary>
	/// Selects screenshots matching the query from the ManicTime screenshot directory.
	/// Returns an empty selection if the directory is missing or empty.
	/// </summary>
	ScreenshotSelection Select(ScreenshotQuery query);

	/// <summary>
	/// Lists screenshots matching the query, returning metadata only (no image bytes).
	/// Each screenshot is registered with <see cref="IScreenshotRegistry"/> for later retrieval.
	/// Supports both interval and activity-transition sampling strategies.
	/// </summary>
	Task<ScreenshotSelection> ListScreenshotsAsync(ScreenshotQuery query, CancellationToken cancellationToken = default);

	/// <summary>
	/// Reads the bytes of a screenshot file after validating the path is safe.
	/// Returns null if the file does not exist, is not a .jpg, or fails security checks.
	/// </summary>
	byte[]? ReadScreenshot(string filePath);
}
