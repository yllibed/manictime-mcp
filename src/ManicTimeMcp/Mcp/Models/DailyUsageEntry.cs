namespace ManicTimeMcp.Mcp.Models;

/// <summary>Projected daily usage entry with minutes instead of raw seconds.</summary>
internal sealed class DailyUsageEntry
{
	/// <summary>Day (yyyy-MM-dd).</summary>
	public required string Day { get; init; }

	/// <summary>Application/website/document name.</summary>
	public required string Name { get; init; }

	/// <summary>Group color.</summary>
	public string? Color { get; init; }

	/// <summary>Group key.</summary>
	public string? Key { get; init; }

	/// <summary>Total usage in minutes.</summary>
	public required double TotalMinutes { get; init; }
}
