using ManicTimeMcp.Mcp;

namespace ManicTimeMcp.Repl;

/// <summary>Adapts transport-neutral tool payloads to Repl-friendly results during the migration.</summary>
internal static class ReplToolResultAdapter
{
	/// <summary>Converts a transport-neutral tool payload to a Repl return payload.</summary>
	public static object FromToolResult(ToolInvocationResult result) =>
		ToolResults.ToReplResult(result);
}
