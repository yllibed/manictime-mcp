using System.ComponentModel;
using Repl.Parameters;

namespace ManicTimeMcp.Repl;

/// <summary>Reusable crop options for screenshot workflows.</summary>
[ReplOptionsGroup]
public sealed class ScreenshotCropOptions
{
	/// <summary>Gets or sets the left crop edge.</summary>
	[ReplOption(Name = "cropX")]
	[Description("Left edge of the crop rectangle.")]
	public double? CropX { get; set; }

	/// <summary>Gets or sets the top crop edge.</summary>
	[ReplOption(Name = "cropY")]
	[Description("Top edge of the crop rectangle.")]
	public double? CropY { get; set; }

	/// <summary>Gets or sets the crop width.</summary>
	[ReplOption(Name = "cropWidth")]
	[Description("Width of the crop rectangle.")]
	public double? CropWidth { get; set; }

	/// <summary>Gets or sets the crop height.</summary>
	[ReplOption(Name = "cropHeight")]
	[Description("Height of the crop rectangle.")]
	public double? CropHeight { get; set; }

	/// <summary>Gets or sets the coordinate units for the crop rectangle.</summary>
	[ReplOption(Name = "coordinateUnits")]
	[Description("Coordinate units for crop values: percent or normalized.")]
	public string CoordinateUnits { get; set; } = "percent";
}
