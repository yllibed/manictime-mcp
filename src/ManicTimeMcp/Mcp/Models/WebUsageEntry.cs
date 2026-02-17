namespace ManicTimeMcp.Mcp.Models;

/// <summary>Website usage entry.</summary>
internal sealed class WebUsageEntry
{
	/// <summary>Website name.</summary>
	public required string Name { get; init; }

	/// <summary>Total minutes.</summary>
	public required double TotalMinutes { get; init; }
}
