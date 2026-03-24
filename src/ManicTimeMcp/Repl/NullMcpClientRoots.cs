using Repl.Mcp;

namespace ManicTimeMcp.Repl;

/// <summary>
/// Fallback MCP roots service used outside an active MCP session.
/// </summary>
internal sealed class NullMcpClientRoots : IMcpClientRoots
{
	private McpClientRoot[] _softRoots = [];

	/// <inheritdoc />
	public bool IsSupported => false;

	/// <inheritdoc />
	public bool HasSoftRoots => _softRoots.Length > 0;

	/// <inheritdoc />
	public IReadOnlyList<McpClientRoot> Current => _softRoots;

	/// <inheritdoc />
	public ValueTask<IReadOnlyList<McpClientRoot>> GetAsync(CancellationToken cancellationToken = default) =>
		ValueTask.FromResult(Current);

	/// <inheritdoc />
	public void SetSoftRoots(IEnumerable<McpClientRoot> roots)
	{
		ArgumentNullException.ThrowIfNull(roots);
		_softRoots = roots.ToArray();
	}

	/// <inheritdoc />
	public void ClearSoftRoots()
	{
		_softRoots = [];
	}
}
