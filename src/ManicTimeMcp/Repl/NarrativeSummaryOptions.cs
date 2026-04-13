using System.ComponentModel;
using Repl.Parameters;

namespace ManicTimeMcp.Repl;

/// <summary>Options for narrative summary generation.</summary>
[ReplOptionsGroup]
public sealed class NarrativeSummaryOptions
{
	/// <summary>Gets or sets a value indicating whether website usage should be included.</summary>
	[ReplOption(Name = "includeWebsites")]
	[Description("Include website usage in the narrative summary.")]
	public bool IncludeWebsites { get; set; } = true;

	/// <summary>Gets or sets the minimum segment duration in minutes.</summary>
	[ReplOption(Name = "minDurationMinutes")]
	[Description("Minimum segment duration in minutes. Use to filter short app flickers (e.g. 2 to keep only sustained activity).")]
	public double MinDurationMinutes { get; set; }

	/// <summary>Gets or sets the maximum merge gap in minutes between same-application segments.</summary>
	[ReplOption(Name = "maxGapMinutes")]
	[Description("Maximum gap in minutes between same-application segments before they stop merging.")]
	public double MaxGapMinutes { get; set; } = 2.0;

	/// <summary>Gets or sets a value indicating whether aggregate summary sections should be included.</summary>
	[ReplOption(Name = "includeSummary")]
	[Description("Include top applications and websites in the narrative response.")]
	public bool IncludeSummary { get; set; }

	/// <summary>Gets or sets the optional cap applied to returned segments.</summary>
	[ReplOption(Name = "maxSegments")]
	[Description("Optional maximum number of returned segments.")]
	public int? MaxSegments { get; set; }
}
