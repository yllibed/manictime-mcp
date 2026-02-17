using System.Collections.Concurrent;

namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Thread-safe, in-memory registry mapping screenshot references to metadata.
/// Backed by <see cref="ConcurrentDictionary{TKey,TValue}"/>.
/// </summary>
public sealed class ScreenshotRegistry : IScreenshotRegistry
{
	private readonly ConcurrentDictionary<string, ScreenshotInfo> _entries = new(StringComparer.Ordinal);

	/// <inheritdoc />
	public string Register(ScreenshotInfo info)
	{
		var refId = ScreenshotRef.Create(info);
		_entries.TryAdd(refId, info);
		return refId;
	}

	/// <inheritdoc />
	public ScreenshotInfo? TryResolve(string screenshotRef) =>
		_entries.TryGetValue(screenshotRef, out var info) ? info : null;
}
