using ManicTimeMcp.Models;

namespace ManicTimeMcp.Database;

/// <summary>Result of schema validation against the manifest.</summary>
public sealed record SchemaValidationResult
{
	/// <summary>Overall validation status.</summary>
	public required SchemaValidationStatus Status { get; init; }

	/// <summary>Schema issues found during validation, empty if valid.</summary>
	public required IReadOnlyList<ValidationIssue> Issues { get; init; }

	/// <summary>Capability matrix built from actual table presence. Null if validation was not performed.</summary>
	public QueryCapabilityMatrix? Capabilities { get; init; }
}
