using ManicTimeMcp.Models;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Configuration;

/// <summary>Source-generated structured log messages for configuration and health diagnostics.</summary>
internal static partial class Log
{
	[LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "Data directory resolved from {Source}: {Path}")]
	internal static partial void DataDirectoryResolved(this ILogger logger, DataDirectorySource source, string path);

	[LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "Data directory could not be resolved through any known method")]
	internal static partial void DataDirectoryUnresolved(this ILogger logger);

	[LoggerMessage(EventId = 1003, Level = LogLevel.Error, Message = "Database file not found: {DatabasePath}")]
	internal static partial void DatabaseNotFound(this ILogger logger, string databasePath);

	[LoggerMessage(EventId = 1004, Level = LogLevel.Warning, Message = "ManicTime process is not running")]
	internal static partial void ManicTimeProcessNotRunning(this ILogger logger);

	[LoggerMessage(EventId = 1005, Level = LogLevel.Warning, Message = "Screenshot directory not found: {ScreenshotPath}")]
	internal static partial void ScreenshotDirectoryAbsent(this ILogger logger, string screenshotPath);

	[LoggerMessage(EventId = 1006, Level = LogLevel.Warning, Message = "Screenshot directory is empty: {ScreenshotPath}")]
	internal static partial void ScreenshotDirectoryEmpty(this ILogger logger, string screenshotPath);

	[LoggerMessage(EventId = 1007, Level = LogLevel.Information, Message = "Health check completed: {Status} ({IssueCount} issues)")]
	internal static partial void HealthCheckCompleted(this ILogger logger, HealthStatus status, int issueCount);
}
