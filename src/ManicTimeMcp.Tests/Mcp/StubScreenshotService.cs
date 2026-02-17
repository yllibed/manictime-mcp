using ManicTimeMcp.Screenshots;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubScreenshotService(ScreenshotSelection? selection = null, byte[]? readResult = null) : IScreenshotService
{
	public ScreenshotSelection Select(ScreenshotQuery query) =>
		selection ?? new ScreenshotSelection { Screenshots = [], TotalMatching = 0, IsTruncated = false };

	public Task<ScreenshotSelection> ListScreenshotsAsync(ScreenshotQuery query, CancellationToken cancellationToken = default) =>
		Task.FromResult(Select(query));

	public byte[]? ReadScreenshot(string filePath) => readResult;
}
