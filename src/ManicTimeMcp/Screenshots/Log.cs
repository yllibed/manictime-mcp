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

	[LoggerMessage(EventId = 3005, Level = LogLevel.Warning, Message = "Failed to decode JPEG for crop operation")]
	internal static partial void CropDecodeFailed(this ILogger logger, Exception? exception);

	[LoggerMessage(EventId = 3006, Level = LogLevel.Warning, Message = "Crop region resolved to empty area: x={X}, y={Y}, w={Width}, h={Height}")]
	internal static partial void CropEmptyRegion(this ILogger logger, double x, double y, double width, double height);

	[LoggerMessage(EventId = 3007, Level = LogLevel.Warning, Message = "Failed to extract bitmap subset for crop")]
	internal static partial void CropExtractFailed(this ILogger logger);

	[LoggerMessage(EventId = 3008, Level = LogLevel.Warning, Message = "Screenshot write path traversal attempt blocked: {FilePath}")]
	internal static partial void ScreenshotWritePathTraversalBlocked(this ILogger logger, string filePath);

	[LoggerMessage(EventId = 3009, Level = LogLevel.Warning, Message = "Screenshot write rejected for invalid extension: {FilePath}")]
	internal static partial void ScreenshotWriteInvalidExtension(this ILogger logger, string filePath);

	[LoggerMessage(EventId = 3010, Level = LogLevel.Information, Message = "Screenshot saved: {FilePath} ({Size} bytes)")]
	internal static partial void ScreenshotSaved(this ILogger logger, string filePath, long size);

	[LoggerMessage(EventId = 3011, Level = LogLevel.Warning, Message = "Failed to write screenshot: {FilePath}")]
	internal static partial void ScreenshotWriteFailed(this ILogger logger, string filePath, Exception exception);
}
