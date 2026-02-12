namespace ManicTimeMcp.Models;

/// <summary>A single health or installation issue with a stable code and optional remediation.</summary>
public sealed record ValidationIssue
{
	/// <summary>Stable machine-readable issue code.</summary>
	public required IssueCode Code { get; init; }

	/// <summary>Fatal or warning classification.</summary>
	public required ValidationSeverity Severity { get; init; }

	/// <summary>Human-readable description of the issue.</summary>
	public required string Message { get; init; }

	/// <summary>Optional remediation hint for the user or MCP client.</summary>
	public string? Remediation { get; init; }
}
