namespace ManicTimeMcp.Mcp.Models;

/// <summary>Response for the get_website_usage tool.</summary>
internal sealed class WebsiteUsageResponse
{
	/// <summary>Breakdown granularity: "hour" or "day".</summary>
	public required string BreakdownGranularity { get; init; }

	/// <summary>Website usage entries.</summary>
	public required List<WebsiteBreakdown> Websites { get; init; }

	/// <summary>Truncation info.</summary>
	public required TruncationInfo Truncation { get; init; }

	/// <summary>Diagnostics info.</summary>
	public required DiagnosticsInfo Diagnostics { get; init; }
}
