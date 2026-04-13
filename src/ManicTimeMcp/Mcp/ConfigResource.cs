namespace ManicTimeMcp.Mcp;

/// <summary>Resource payload for configuration information.</summary>
public sealed record ConfigResource
{
	/// <summary>Resolved data directory path.</summary>
	public string? DataDirectory { get; init; }

	/// <summary>How the data directory was resolved.</summary>
	public string? DirectorySource { get; init; }
}
