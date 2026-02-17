namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Crops a JPEG image to a specified region of interest.
/// </summary>
public interface ICropService
{
	/// <summary>
	/// Crops the given JPEG image bytes to the specified region.
	/// Returns the cropped JPEG bytes, or null if cropping fails.
	/// </summary>
	byte[]? Crop(byte[] jpeg, CropRegion region);
}
