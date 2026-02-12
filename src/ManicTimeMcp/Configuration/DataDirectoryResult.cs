using ManicTimeMcp.Models;

namespace ManicTimeMcp.Configuration;

/// <summary>Result of the ManicTime data directory resolution.</summary>
public sealed record DataDirectoryResult
{
	/// <summary>Resolved directory path, or null if resolution failed.</summary>
	public required string? Path { get; init; }

	/// <summary>Which method produced the resolved path.</summary>
	public required DataDirectorySource Source { get; init; }
}
