using System.ComponentModel;
using Repl.Parameters;

namespace ManicTimeMcp.Repl;

/// <summary>Options for daily summary generation.</summary>
[ReplOptionsGroup]
public sealed class DailySummaryOptions
{
	/// <summary>Gets or sets a value indicating whether segments are included.</summary>
	[ReplOption(Name = "includeSegments")]
	[Description("Include detailed activity segments in the response.")]
	public bool IncludeSegments { get; set; } = true;

	/// <summary>Gets or sets the minimum segment duration in minutes.</summary>
	[ReplOption(Name = "minDurationMinutes")]
	[Description("Minimum segment duration in minutes.")]
	public double MinDurationMinutes { get; set; }

	/// <summary>Gets or sets a value indicating whether hourly website detail is included.</summary>
	[ReplOption(Name = "includeHourlyWebBreakdown")]
	[Description("Include hourly website breakdown details.")]
	public bool IncludeHourlyWebBreakdown { get; set; }

	/// <summary>Gets or sets the maximum merge gap in minutes between same-application segments.</summary>
	[ReplOption(Name = "maxGapMinutes")]
	[Description("Maximum gap in minutes between same-application segments before they stop merging.")]
	public double MaxGapMinutes { get; set; } = 2.0;

	/// <summary>Gets or sets the optional cap applied to returned segments.</summary>
	[ReplOption(Name = "maxSegments")]
	[Description("Optional maximum number of returned segments.")]
	public int? MaxSegments { get; set; }
}
