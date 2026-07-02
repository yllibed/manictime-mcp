using System.ComponentModel;
using Repl.Parameters;

namespace ManicTimeMcp.Repl;

/// <summary>Options for the consolidated usage summary command.</summary>
[ReplOptionsGroup]
public sealed class UsageSummaryOptions
{
	/// <summary>Gets or sets the usage type filter.</summary>
	[ReplOption(Name = "type")]
	[Description("Filter to a specific type: 'applications', 'websites', 'documents', 'tags', or 'all' (default).")]
	public string Type { get; set; } = "all";

	/// <summary>Gets or sets the maximum number of items per section.</summary>
	[ReplOption(Name = "limit")]
	[Description("Maximum items per section. Omit for server default (typically 1000).")]
	public int? Limit { get; set; }

	/// <summary>Gets or sets the minimum total minutes to keep a website in the output.</summary>
	[ReplOption(Name = "minMinutes")]
	[Description("Minimum total minutes to include an item (applies to websites). Default 0.5.")]
	public double MinMinutes { get; set; } = 0.5;
}
