using System.ComponentModel;
using Repl.Parameters;

namespace ManicTimeMcp.Repl;

/// <summary>Options for screenshot saving.</summary>
[ReplOptionsGroup]
public sealed class ScreenshotSaveOptions
{
	/// <summary>Gets or sets the preferred output path.</summary>
	[ReplOption(Name = "outputPath")]
	[Description("Preferred relative output path inside an MCP root.")]
	public string? OutputPath { get; set; }
}
