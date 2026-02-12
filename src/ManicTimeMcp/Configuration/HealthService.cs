using ManicTimeMcp.Database;
using ManicTimeMcp.Models;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Configuration;

/// <summary>
/// Produces a <see cref="HealthReport"/> by checking the data directory, database,
/// ManicTime process, and screenshot availability.
/// </summary>
public sealed class HealthService : IHealthService
{
	internal const string DatabaseFileName = "ManicTimeReports.db";
	internal const string ScreenshotDirectoryName = "Screenshots";
	internal const string ManicTimeProcessName = "ManicTime";
	internal const string ScreenshotSearchPattern = "*.jpg";
	internal const string ScreenshotRemediationHint = "Review ManicTime screenshot capture and retention settings.";

	private readonly IDataDirectoryResolver _resolver;
	private readonly IPlatformEnvironment _platform;
	private readonly ISchemaValidator _schemaValidator;
	private readonly ILogger<HealthService> _logger;

	/// <summary>Creates a new health service with injected dependencies.</summary>
	public HealthService(
		IDataDirectoryResolver resolver,
		IPlatformEnvironment platform,
		ISchemaValidator schemaValidator,
		ILogger<HealthService> logger)
	{
		_resolver = resolver;
		_platform = platform;
		_schemaValidator = schemaValidator;
		_logger = logger;
	}

	/// <inheritdoc />
	public HealthReport GetHealthReport()
	{
		var directoryResult = _resolver.Resolve();
		var issues = new List<ValidationIssue>();

		CheckDataDirectory(directoryResult, issues);
		var (dbExists, dbSize) = CheckDatabase(directoryResult.Path, issues);
		var schemaStatus = CheckSchema(directoryResult.Path, dbExists, issues);
		var processRunning = CheckProcess(issues);
		var screenshots = CheckScreenshots(directoryResult.Path, issues);

		var status = DeriveStatus(issues);

		_logger.HealthCheckCompleted(status, issues.Count);

		return new HealthReport
		{
			Status = status,
			DataDirectory = directoryResult.Path,
			DirectorySource = directoryResult.Source,
			DatabaseExists = dbExists,
			DatabaseSizeBytes = dbSize,
			SchemaStatus = schemaStatus,
			ManicTimeProcessRunning = processRunning,
			Screenshots = screenshots,
			Issues = issues.AsReadOnly(),
		};
	}

	/// <summary>Pure status derivation from the issue list — testable without side effects.</summary>
	internal static HealthStatus DeriveStatus(IReadOnlyList<ValidationIssue> issues)
	{
		var hasFatal = false;
		var hasWarning = false;

		foreach (var issue in issues)
		{
			switch (issue.Severity)
			{
				case ValidationSeverity.Fatal:
					hasFatal = true;
					break;
				case ValidationSeverity.Warning:
					hasWarning = true;
					break;
			}
		}

		if (hasFatal)
		{
			return HealthStatus.Unhealthy;
		}

		return hasWarning ? HealthStatus.Degraded : HealthStatus.Healthy;
	}

	private static void CheckDataDirectory(DataDirectoryResult directoryResult, List<ValidationIssue> issues)
	{
		if (directoryResult.Source == DataDirectorySource.Unresolved)
		{
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.DataDirectoryUnresolved,
				Severity = ValidationSeverity.Fatal,
				Message = "ManicTime data directory could not be resolved.",
				Remediation = "Set the MANICTIME_DATA_DIR environment variable to the ManicTime data directory path.",
			});
		}
	}

	private SchemaValidationStatus CheckSchema(string? dataDirectory, bool dbExists, List<ValidationIssue> issues)
	{
		if (!dbExists || dataDirectory is null)
		{
			return SchemaValidationStatus.NotChecked;
		}

		var dbPath = Path.Combine(dataDirectory, DatabaseFileName);
		var result = _schemaValidator.Validate(dbPath);
		issues.AddRange(result.Issues);
		return result.Status;
	}

	private (bool Exists, long? SizeBytes) CheckDatabase(string? dataDirectory, List<ValidationIssue> issues)
	{
		if (dataDirectory is null)
		{
			return (false, null);
		}

		var dbPath = Path.Combine(dataDirectory, DatabaseFileName);

		if (!_platform.FileExists(dbPath))
		{
			_logger.DatabaseNotFound(dbPath);
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.DatabaseNotFound,
				Severity = ValidationSeverity.Fatal,
				Message = $"Database file '{DatabaseFileName}' not found in '{dataDirectory}'.",
				Remediation = "Verify that ManicTime is installed and has been run at least once.",
			});
			return (false, null);
		}

		var size = _platform.GetFileSize(dbPath);
		return (true, size);
	}

	private bool CheckProcess(List<ValidationIssue> issues)
	{
		var running = _platform.IsProcessRunning(ManicTimeProcessName);

		if (!running)
		{
			_logger.ManicTimeProcessNotRunning();
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.ManicTimeProcessNotRunning,
				Severity = ValidationSeverity.Warning,
				Message = "ManicTime desktop process is not currently running.",
				Remediation = "Start ManicTime to enable real-time data collection.",
			});
		}

		return running;
	}

	private ScreenshotAvailability CheckScreenshots(string? dataDirectory, List<ValidationIssue> issues)
	{
		if (dataDirectory is null)
		{
			return new ScreenshotAvailability
			{
				Status = ScreenshotAvailabilityStatus.Unknown,
				Reason = ScreenshotUnavailableReason.Unknown,
				RemediationHint = "Data directory is not resolved; screenshot status cannot be determined.",
			};
		}

		var screenshotDir = Path.Combine(dataDirectory, ScreenshotDirectoryName);

		if (!_platform.DirectoryExists(screenshotDir))
		{
			_logger.ScreenshotDirectoryAbsent(screenshotDir);
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.ScreenshotDirectoryAbsent,
				Severity = ValidationSeverity.Warning,
				Message = $"Screenshot directory not found at '{screenshotDir}'.",
				Remediation = ScreenshotRemediationHint,
			});

			return new ScreenshotAvailability
			{
				Status = ScreenshotAvailabilityStatus.Unavailable,
				Reason = ScreenshotUnavailableReason.CaptureDisabled,
				RemediationHint = ScreenshotRemediationHint,
			};
		}

		if (!_platform.DirectoryHasFiles(screenshotDir, ScreenshotSearchPattern, SearchOption.AllDirectories))
		{
			_logger.ScreenshotDirectoryEmpty(screenshotDir);
			issues.Add(new ValidationIssue
			{
				Code = IssueCode.ScreenshotDirectoryEmpty,
				Severity = ValidationSeverity.Warning,
				Message = "Screenshot directory exists but contains no screenshot files.",
				Remediation = ScreenshotRemediationHint,
			});

			return new ScreenshotAvailability
			{
				Status = ScreenshotAvailabilityStatus.Unavailable,
				Reason = ScreenshotUnavailableReason.Retention,
				RemediationHint = ScreenshotRemediationHint,
			};
		}

		return new ScreenshotAvailability
		{
			Status = ScreenshotAvailabilityStatus.Available,
			Reason = ScreenshotUnavailableReason.None,
		};
	}
}
