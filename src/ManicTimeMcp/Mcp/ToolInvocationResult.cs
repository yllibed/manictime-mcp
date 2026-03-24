namespace ManicTimeMcp.Mcp;

/// <summary>Represents a tool execution payload independently of any MCP transport type.</summary>
public sealed class ToolInvocationResult
{
	/// <summary>Creates a new tool invocation result.</summary>
	public ToolInvocationResult(bool isError, string payload, string? errorCode = null)
	{
		IsError = isError;
		Payload = payload;
		ErrorCode = errorCode;
	}

	/// <summary>Gets whether the result represents an error.</summary>
	public bool IsError { get; }

	/// <summary>Gets the textual payload returned by the operation.</summary>
	public string Payload { get; }

	/// <summary>Gets the optional machine-readable error code.</summary>
	public string? ErrorCode { get; }
}
