namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Maps opaque screenshot references to their resolved metadata.
/// References are registered during list operations and resolved during get/crop.
/// </summary>
public interface IScreenshotRegistry
{
	/// <summary>
	/// Registers a screenshot and returns its opaque reference string.
	/// Idempotent — the same info always produces the same ref.
	/// </summary>
	string Register(ScreenshotInfo info);

	/// <summary>
	/// Resolves a reference to its screenshot metadata.
	/// Returns null if the reference has not been registered.
	/// </summary>
	ScreenshotInfo? TryResolve(string screenshotRef);
}
