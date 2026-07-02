namespace ManicTimeMcp.Mcp.Models;

/// <summary>Single item in a usage summary section (application, website, document, or tag).</summary>
internal sealed class UsageSummaryEntry
{
	/// <summary>Item name (application, domain, file path, or tag name).</summary>
	public required string Name { get; init; }

	/// <summary>Group color when available.</summary>
	public string? Color { get; init; }

	/// <summary>Total usage in minutes.</summary>
	public required double TotalMinutes { get; init; }
}
