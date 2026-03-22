using Repl.Mcp;

namespace ManicTimeMcp.Tests.Repl;

internal sealed class TestMcpClientRoots : IMcpClientRoots
{
	private readonly bool _isSupported;
	private readonly List<McpClientRoot> _nativeRoots;
	private List<McpClientRoot> _softRoots;

	public TestMcpClientRoots(
		IEnumerable<McpClientRoot>? current = null,
		bool isSupported = false)
	{
		_isSupported = isSupported;
		_nativeRoots = current?.ToList() ?? [];
		_softRoots = [];
	}

	public bool IsSupported => _isSupported;

	public bool HasSoftRoots => _softRoots.Count > 0;

	public IReadOnlyList<McpClientRoot> Current =>
		_nativeRoots.Count > 0 ? _nativeRoots : _softRoots;

	public ValueTask<IReadOnlyList<McpClientRoot>> GetAsync(CancellationToken cancellationToken = default) =>
		ValueTask.FromResult(Current);

	public void SetSoftRoots(IEnumerable<McpClientRoot> roots)
	{
		ArgumentNullException.ThrowIfNull(roots);
		_softRoots = roots.ToList();
	}

	public void ClearSoftRoots() => _softRoots.Clear();
}
