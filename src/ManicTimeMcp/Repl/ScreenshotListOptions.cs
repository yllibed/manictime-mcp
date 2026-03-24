using System.ComponentModel;
using Repl.Parameters;

namespace ManicTimeMcp.Repl;

/// <summary>Options for screenshot listing queries.</summary>
[ReplOptionsGroup]
public sealed class ScreenshotListOptions
{
	/// <summary>Gets or sets the maximum number of screenshots to return.</summary>
	[ReplOption(Name = "maxCount")]
	[Description("Maximum number of screenshots to return.")]
	public int? MaxCount { get; set; }

	/// <summary>Gets or sets the sampling strategy.</summary>
	[ReplOption(Name = "samplingStrategy")]
	[Description("Sampling strategy: activity_transition or interval.")]
	public string SamplingStrategy { get; set; } = "activity_transition";
}
