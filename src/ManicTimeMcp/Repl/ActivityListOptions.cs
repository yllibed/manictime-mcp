using System.ComponentModel;
using Repl.Parameters;

namespace ManicTimeMcp.Repl;

/// <summary>Options for activity list queries.</summary>
[ReplOptionsGroup]
public sealed class ActivityListOptions
{
	/// <summary>Gets or sets the maximum number of returned rows.</summary>
	[ReplOption(Name = "limit")]
	[Description("Maximum number of returned rows.")]
	public int? Limit { get; set; }

	/// <summary>Gets or sets a value indicating whether group metadata is included.</summary>
	[ReplOption(Name = "includeGroupDetails")]
	[Description("Include resolved group metadata such as names, keys, and colors.")]
	public bool IncludeGroupDetails { get; set; } = true;
}
