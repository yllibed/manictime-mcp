using ManicTimeMcp.Screenshots;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubScreenshotService(
	ScreenshotSelection? selection = null,
	byte[]? readResult = null,
	long writeResult = 0) : IScreenshotService
{
	public ScreenshotSelection Select(ScreenshotQuery query) =>
		selection ?? new ScreenshotSelection { Screenshots = [], TotalMatching = 0, IsTruncated = false };

	public Task<ScreenshotSelection> ListScreenshotsAsync(ScreenshotQuery query, CancellationToken cancellationToken = default) =>
		Task.FromResult(Select(query));

	public byte[]? ReadScreenshot(string filePath) => readResult;

	public long WriteScreenshot(byte[] data, string outputPath, string allowedRootDirectory) => writeResult;
}
