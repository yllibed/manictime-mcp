using ManicTimeMcp.Models;

namespace ManicTimeMcp.Database;

/// <summary>Validates the ManicTimeReports.db schema against the expected manifest.</summary>
public interface ISchemaValidator
{
	/// <summary>
	/// Checks the database schema and returns the validation result.
	/// Returns <see cref="SchemaValidationResult"/> with status and any issues found.
	/// </summary>
	SchemaValidationResult Validate(string databasePath);
}
