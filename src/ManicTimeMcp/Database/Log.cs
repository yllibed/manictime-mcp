using ManicTimeMcp.Models;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>Source-generated structured log messages for database operations.</summary>
internal static partial class Log
{
	[LoggerMessage(EventId = 2001, Level = LogLevel.Debug, Message = "Database connection created for {DatabasePath}")]
	internal static partial void DatabaseConnectionCreated(this ILogger logger, string databasePath);

	[LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "Required table '{TableName}' is missing from the database schema")]
	internal static partial void SchemaTableMissing(this ILogger logger, string tableName);

	[LoggerMessage(EventId = 2003, Level = LogLevel.Error, Message = "Required column '{ColumnName}' is missing from table '{TableName}'")]
	internal static partial void SchemaColumnMissing(this ILogger logger, string tableName, string columnName);

	[LoggerMessage(EventId = 2004, Level = LogLevel.Information, Message = "Schema validation completed: {Status} ({IssueCount} issues)")]
	internal static partial void SchemaValidationCompleted(this ILogger logger, SchemaValidationStatus status, int issueCount);

	[LoggerMessage(EventId = 2005, Level = LogLevel.Warning, Message = "Transient SQLITE_BUSY encountered, retrying (attempt {Attempt}/{MaxAttempts})")]
	internal static partial void SqliteBusyRetry(this ILogger logger, int attempt, int maxAttempts);

	[LoggerMessage(EventId = 2006, Level = LogLevel.Debug, Message = "Query executed: {QueryName} returned {RowCount} rows")]
	internal static partial void QueryExecuted(this ILogger logger, string queryName, int rowCount);
}
