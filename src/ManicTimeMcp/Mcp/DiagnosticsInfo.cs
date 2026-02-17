namespace ManicTimeMcp.Mcp;

/// <summary>Diagnostics metadata for degraded capabilities in tool responses.</summary>
internal sealed record DiagnosticsInfo
{
	/// <summary>Whether the result was computed using a fallback/degraded path.</summary>
	public required bool Degraded { get; init; }

	/// <summary>Machine-readable reason code when degraded.</summary>
	public string? ReasonCode { get; init; }

	/// <summary>Human-readable remediation hint when degraded.</summary>
	public string? RemediationHint { get; init; }

	/// <summary>Non-degraded diagnostics instance.</summary>
	internal static DiagnosticsInfo Ok { get; } = new() { Degraded = false };
}
