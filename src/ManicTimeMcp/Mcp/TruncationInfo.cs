namespace ManicTimeMcp.Mcp;

/// <summary>Standard truncation metadata for tool responses with hard caps.</summary>
internal sealed record TruncationInfo
{
	/// <summary>Whether the result was truncated.</summary>
	public required bool Truncated { get; init; }

	/// <summary>Number of items returned.</summary>
	public required int ReturnedCount { get; init; }

	/// <summary>Total available items, or null if unknown.</summary>
	public int? TotalAvailable { get; init; }
}
