namespace ManicTimeMcp.Models;

/// <summary>Stable machine-readable codes for health and installation diagnostics.</summary>
public enum IssueCode
{
	/// <summary>Data directory could not be resolved through any known method.</summary>
	DataDirectoryUnresolved,

	/// <summary>ManicTimeReports.db was not found in the data directory.</summary>
	DatabaseNotFound,

	/// <summary>Required database schema tables or columns are missing or incompatible.</summary>
	SchemaValidationFailed,

	/// <summary>SQLite integrity check did not return ok.</summary>
	DatabaseIntegrityCheckFailed,

	/// <summary>The ManicTime desktop process is not currently running.</summary>
	ManicTimeProcessNotRunning,

	/// <summary>The screenshots directory does not exist.</summary>
	ScreenshotDirectoryAbsent,

	/// <summary>The screenshots directory exists but contains no screenshot files.</summary>
	ScreenshotDirectoryEmpty,
}
