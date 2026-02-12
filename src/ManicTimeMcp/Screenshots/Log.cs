using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Screenshots;

/// <summary>Source-generated structured log messages for screenshot operations.</summary>
internal static partial class Log
{
	[LoggerMessage(EventId = 3001, Level = LogLevel.Debug, Message = "Selected {Count} screenshots from {TotalMatching} matching")]
	internal static partial void ScreenshotsSelected(this ILogger logger, int count, int totalMatching);

	[LoggerMessage(EventId = 3002, Level = LogLevel.Warning, Message = "Screenshot path traversal attempt blocked: {FilePath}")]
	internal static partial void ScreenshotPathTraversalBlocked(this ILogger logger, string filePath);

	[LoggerMessage(EventId = 3003, Level = LogLevel.Warning, Message = "Screenshot read rejected for non-.jpg extension: {FilePath}")]
	internal static partial void ScreenshotInvalidExtension(this ILogger logger, string filePath);

	[LoggerMessage(EventId = 3004, Level = LogLevel.Warning, Message = "Failed to read screenshot file: {FilePath}")]
	internal static partial void ScreenshotReadFailed(this ILogger logger, string filePath, Exception exception);
}
