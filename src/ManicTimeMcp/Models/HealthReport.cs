namespace ManicTimeMcp.Models;

/// <summary>Complete health diagnostic payload for the ManicTime MCP environment.</summary>
public sealed record HealthReport
{
	/// <summary>Overall health status derived from the issue list.</summary>
	public required HealthStatus Status { get; init; }

	/// <summary>Resolved data directory path, or null if unresolved.</summary>
	public required string? DataDirectory { get; init; }

	/// <summary>How the data directory was resolved.</summary>
	public required DataDirectorySource DirectorySource { get; init; }

	/// <summary>Whether ManicTimeReports.db exists in the data directory.</summary>
	public required bool DatabaseExists { get; init; }

	/// <summary>Size of ManicTimeReports.db in bytes, or null if the file does not exist.</summary>
	public required long? DatabaseSizeBytes { get; init; }

	/// <summary>Result of database schema validation.</summary>
	public required SchemaValidationStatus SchemaStatus { get; init; }

	/// <summary>Whether the ManicTime desktop process is currently running.</summary>
	public required bool ManicTimeProcessRunning { get; init; }

	/// <summary>Process ID of the running ManicTime instance, or null if not running.</summary>
	public required int? ManicTimeProcessId { get; init; }

	/// <summary>Installed ManicTime product version (e.g. "2025.3.5.0"), or null if not detected.</summary>
	public required string? ManicTimeVersion { get; init; }

	/// <summary>Screenshot directory availability details.</summary>
	public required ScreenshotAvailability Screenshots { get; init; }

	/// <summary>MCP server version from AssemblyInformationalVersion.</summary>
	public string? McpServerVersion { get; init; }

	/// <summary>Capability statuses including fallback information.</summary>
	public IReadOnlyList<CapabilityStatus>? Capabilities { get; init; }

	/// <summary>All detected issues ordered by severity (fatal first, then warnings).</summary>
	public required IReadOnlyList<ValidationIssue> Issues { get; init; }
}
