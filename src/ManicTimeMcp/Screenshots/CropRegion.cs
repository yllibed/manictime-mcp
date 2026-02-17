namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Defines a rectangular crop region in percentage or normalized coordinates.
/// </summary>
public sealed record CropRegion
{
	/// <summary>Left edge of the crop region.</summary>
	public required double X { get; init; }

	/// <summary>Top edge of the crop region.</summary>
	public required double Y { get; init; }

	/// <summary>Width of the crop region.</summary>
	public required double Width { get; init; }

	/// <summary>Height of the crop region.</summary>
	public required double Height { get; init; }

	/// <summary>Coordinate system used for the region values.</summary>
	public CoordinateUnits Units { get; init; } = CoordinateUnits.Percent;
}
