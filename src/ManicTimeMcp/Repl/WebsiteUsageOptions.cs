using System.ComponentModel;
using Repl.Parameters;

namespace ManicTimeMcp.Repl;

/// <summary>Options for website usage queries.</summary>
[ReplOptionsGroup]
public sealed class WebsiteUsageOptions
{
	/// <summary>Gets or sets the maximum number of returned websites.</summary>
	[ReplOption(Name = "limit")]
	[Description("Maximum number of websites to return.")]
	public int? Limit { get; set; }

	/// <summary>Gets or sets the minimum total minutes required to keep a website in the output.</summary>
	[ReplOption(Name = "minMinutes")]
	[Description("Minimum total number of minutes required to keep a website in the output.")]
	public double MinMinutes { get; set; } = 0.5;
}
