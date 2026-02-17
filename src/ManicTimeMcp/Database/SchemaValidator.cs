using ManicTimeMcp.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>
/// Validates the ManicTimeReports.db schema against <see cref="SchemaManifest"/>.
/// Core tables missing = Fatal, supplemental/informational missing = Warning.
/// Builds a <see cref="QueryCapabilityMatrix"/> from actual table presence.
/// </summary>
public sealed class SchemaValidator : ISchemaValidator
{
	private readonly ILogger<SchemaValidator> _logger;

	/// <summary>Creates a new schema validator.</summary>
	public SchemaValidator(ILogger<SchemaValidator> logger)
	{
		_logger = logger;
	}

	/// <inheritdoc />
	public SchemaValidationResult Validate(string databasePath)
	{
		var issues = new List<ValidationIssue>();

		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = databasePath,
			Mode = SqliteOpenMode.ReadOnly,
		}.ToString();

		using var connection = new SqliteConnection(connectionString);
		connection.Open();

		var existingTables = GetTableNames(connection);
		var tablesWithColumnIssues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var hasFatalIssue = false;
		var hasWarning = false;

		foreach (var (tableName, definition) in SchemaManifest.Tables)
		{
			ValidateTable(connection, tableName, definition, existingTables,
				tablesWithColumnIssues, issues, ref hasFatalIssue, ref hasWarning);
		}

		// Build capabilities from tables that exist AND pass column validation.
		// A supplemental table with missing columns is not a usable capability.
		var validatedTables = new HashSet<string>(existingTables, StringComparer.OrdinalIgnoreCase);
		validatedTables.ExceptWith(tablesWithColumnIssues);
		var capabilities = new QueryCapabilityMatrix(validatedTables);
		var status = DeriveStatus(hasFatalIssue, hasWarning);

		_logger.SchemaValidationCompleted(status, issues.Count);

		return new SchemaValidationResult
		{
			Status = status,
			Issues = issues.AsReadOnly(),
			Capabilities = capabilities,
		};
	}

	private void ValidateTable(
		SqliteConnection connection,
		string tableName,
		TableDefinition definition,
		HashSet<string> existingTables,
		HashSet<string> tablesWithColumnIssues,
		List<ValidationIssue> issues,
		ref bool hasFatalIssue,
		ref bool hasWarning)
	{
		var (severity, issueCode) = GetSeverityForTier(definition.Tier);

		if (!existingTables.Contains(tableName))
		{
			LogTableMissing(tableName, definition.Tier);
			issues.Add(new ValidationIssue
			{
				Code = issueCode,
				Severity = severity,
				Message = $"Table '{tableName}' is missing from the database.",
				Remediation = GetTableMissingRemediation(definition.Tier),
			});
			TrackSeverity(severity, ref hasFatalIssue, ref hasWarning);
			return;
		}

		var existingColumns = GetColumnNames(connection, tableName);
		foreach (var requiredColumn in definition.RequiredColumns)
		{
			if (!existingColumns.Contains(requiredColumn))
			{
				LogColumnMissing(tableName, requiredColumn, definition.Tier);
				issues.Add(new ValidationIssue
				{
					Code = GetColumnIssueCode(definition.Tier),
					Severity = severity,
					Message = $"Column '{requiredColumn}' is missing from table '{tableName}'.",
					Remediation = GetColumnMissingRemediation(definition.Tier),
				});
				TrackSeverity(severity, ref hasFatalIssue, ref hasWarning);
				tablesWithColumnIssues.Add(tableName);
			}
		}
	}

	private static (ValidationSeverity Severity, IssueCode Code) GetSeverityForTier(TableTier tier) => tier switch
	{
		TableTier.Core => (ValidationSeverity.Fatal, IssueCode.SchemaValidationFailed),
		TableTier.Supplemental => (ValidationSeverity.Warning, IssueCode.SupplementalTableMissing),
		TableTier.Informational => (ValidationSeverity.Warning, IssueCode.SupplementalTableMissing),
		_ => (ValidationSeverity.Fatal, IssueCode.SchemaValidationFailed),
	};

	private static IssueCode GetColumnIssueCode(TableTier tier) => tier switch
	{
		TableTier.Core => IssueCode.SchemaValidationFailed,
		_ => IssueCode.SupplementalColumnMissing,
	};

	private static string GetTableMissingRemediation(TableTier tier) => tier switch
	{
		TableTier.Core => "This may indicate an incompatible ManicTime version. Verify ManicTime is installed and has been run at least once.",
		_ => "This table enables richer queries. Update ManicTime or run it for a longer period to generate this data.",
	};

	private static string GetColumnMissingRemediation(TableTier tier) => tier switch
	{
		TableTier.Core => "This may indicate schema drift from a ManicTime update. Check ManicTime version compatibility.",
		_ => "This column enables enhanced query results. Consider updating ManicTime.",
	};

	private static void TrackSeverity(ValidationSeverity severity, ref bool hasFatalIssue, ref bool hasWarning)
	{
		if (severity == ValidationSeverity.Fatal)
		{
			hasFatalIssue = true;
		}
		else
		{
			hasWarning = true;
		}
	}

	private static SchemaValidationStatus DeriveStatus(bool hasFatal, bool hasWarning)
	{
		if (hasFatal)
		{
			return SchemaValidationStatus.Invalid;
		}

		return hasWarning ? SchemaValidationStatus.ValidWithWarnings : SchemaValidationStatus.Valid;
	}

	private void LogTableMissing(string tableName, TableTier tier)
	{
		if (tier == TableTier.Core)
		{
			_logger.SchemaTableMissing(tableName);
		}
		else
		{
			_logger.SupplementalTableMissing(tableName);
		}
	}

	private void LogColumnMissing(string tableName, string columnName, TableTier tier)
	{
		if (tier == TableTier.Core)
		{
			_logger.SchemaColumnMissing(tableName, columnName);
		}
		else
		{
			_logger.SupplementalColumnMissing(tableName, columnName);
		}
	}

	private static HashSet<string> GetTableNames(SqliteConnection connection)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY name";

		var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			tables.Add(reader.GetString(0));
		}

		return tables;
	}

	private static HashSet<string> GetColumnNames(SqliteConnection connection, string tableName)
	{
		using var command = connection.CreateCommand();
		command.CommandText = $"PRAGMA table_info('{tableName}')";

		var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		using var reader = command.ExecuteReader();
		while (reader.Read())
		{
			columns.Add(reader.GetString(1)); // Column 1 = "name"
		}

		return columns;
	}
}
