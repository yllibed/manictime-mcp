using System.ComponentModel;
using Repl.Parameters;

namespace ManicTimeMcp.Repl;

/// <summary>Reusable limit options for bounded list queries.</summary>
[ReplOptionsGroup]
public sealed class LimitOptions
{
	/// <summary>Gets or sets the maximum number of returned rows.</summary>
	[ReplOption(Name = "limit")]
	[Description("Maximum rows to return. Omit for server default (typically 1000).")]
	public int? Limit { get; set; }
}
