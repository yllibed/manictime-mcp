using ManicTimeMcp.Models;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>
/// Validates the ManicTimeReports.db schema against <see cref="SchemaManifest"/>.
/// Checks that all required tables exist and contain the expected columns.
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

		foreach (var (tableName, definition) in SchemaManifest.Tables)
		{
			if (!existingTables.Contains(tableName))
			{
				_logger.SchemaTableMissing(tableName);
				issues.Add(new ValidationIssue
				{
					Code = IssueCode.SchemaValidationFailed,
					Severity = ValidationSeverity.Fatal,
					Message = $"Required table '{tableName}' is missing from the database.",
					Remediation = "This may indicate an incompatible ManicTime version. Verify ManicTime is installed and has been run at least once.",
				});
				continue;
			}

			var existingColumns = GetColumnNames(connection, tableName);
			foreach (var requiredColumn in definition.RequiredColumns)
			{
				if (!existingColumns.Contains(requiredColumn))
				{
					_logger.SchemaColumnMissing(tableName, requiredColumn);
					issues.Add(new ValidationIssue
					{
						Code = IssueCode.SchemaValidationFailed,
						Severity = ValidationSeverity.Fatal,
						Message = $"Required column '{requiredColumn}' is missing from table '{tableName}'.",
						Remediation = "This may indicate schema drift from a ManicTime update. Check ManicTime version compatibility.",
					});
				}
			}
		}

		var status = issues.Count == 0
			? SchemaValidationStatus.Valid
			: SchemaValidationStatus.Invalid;

		_logger.SchemaValidationCompleted(status, issues.Count);

		return new SchemaValidationResult
		{
			Status = status,
			Issues = issues.AsReadOnly(),
		};
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
