using Repl.Mcp;

namespace ManicTimeMcp.Repl;

/// <summary>
/// Fallback MCP roots service used outside an active MCP session.
/// </summary>
internal sealed class NullMcpClientRoots : IMcpClientRoots
{
	/// <inheritdoc />
	public bool IsSupported => false;

	/// <inheritdoc />
	public bool HasSoftRoots => false;

	/// <inheritdoc />
	public IReadOnlyList<McpClientRoot> Current => [];

	/// <inheritdoc />
	public ValueTask<IReadOnlyList<McpClientRoot>> GetAsync(CancellationToken cancellationToken = default) =>
		ValueTask.FromResult(Current);

	/// <inheritdoc />
	public void SetSoftRoots(IEnumerable<McpClientRoot> roots)
	{
		ArgumentNullException.ThrowIfNull(roots);
	}

	/// <inheritdoc />
	public void ClearSoftRoots()
	{
	}
}
