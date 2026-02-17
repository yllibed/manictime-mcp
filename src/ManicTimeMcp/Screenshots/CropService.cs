using Microsoft.Extensions.Logging;
using SkiaSharp;

namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Crops JPEG images using SkiaSharp with percentage or normalized coordinate input.
/// Out-of-bounds regions are clamped to valid image bounds.
/// </summary>
public sealed class CropService : ICropService
{
	/// <summary>JPEG encoding quality (0-100).</summary>
	private const int JpegQuality = 85;

	private readonly ILogger<CropService> _logger;

	/// <summary>Creates a new crop service.</summary>
	public CropService(ILogger<CropService> logger)
	{
		_logger = logger;
	}

	/// <inheritdoc />
	public byte[]? Crop(byte[] jpeg, CropRegion region)
	{
		SKBitmap? bitmap;
		try
		{
			bitmap = SKBitmap.Decode(jpeg);
		}
#pragma warning disable CA1031 // Do not catch general exception types — SkiaSharp may throw for invalid/corrupt data
		catch (Exception ex)
#pragma warning restore CA1031
		{
			_logger.CropDecodeFailed(ex);
			return null;
		}

		if (bitmap is null)
		{
			_logger.CropDecodeFailed(exception: null);
			return null;
		}

		using var _ = bitmap;

		var pixelRect = ToPixelRect(bitmap.Width, bitmap.Height, region);
		if (pixelRect.Width <= 0 || pixelRect.Height <= 0)
		{
			_logger.CropEmptyRegion(region.X, region.Y, region.Width, region.Height);
			return null;
		}

		using var subset = new SKBitmap();
		if (!bitmap.ExtractSubset(subset, pixelRect))
		{
			_logger.CropExtractFailed();
			return null;
		}

		using var image = SKImage.FromBitmap(subset);
		using var data = image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality);
		return data.ToArray();
	}

	/// <summary>
	/// Converts percentage or normalized coordinates to pixel coordinates,
	/// clamping to valid image bounds.
	/// </summary>
	internal static SKRectI ToPixelRect(int imageWidth, int imageHeight, CropRegion region)
	{
		double scale = region.Units == CoordinateUnits.Normalized ? 1.0 : 0.01;

		double fx = region.X * scale;
		double fy = region.Y * scale;
		double fw = region.Width * scale;
		double fh = region.Height * scale;

		// Clamp fractions to [0, 1]
		fx = Math.Clamp(fx, 0.0, 1.0);
		fy = Math.Clamp(fy, 0.0, 1.0);
		fw = Math.Clamp(fw, 0.0, 1.0);
		fh = Math.Clamp(fh, 0.0, 1.0);

		int left = (int)Math.Round(fx * imageWidth);
		int top = (int)Math.Round(fy * imageHeight);
		int right = (int)Math.Round((fx + fw) * imageWidth);
		int bottom = (int)Math.Round((fy + fh) * imageHeight);

		// Clamp to image bounds
		left = Math.Clamp(left, 0, imageWidth);
		top = Math.Clamp(top, 0, imageHeight);
		right = Math.Clamp(right, 0, imageWidth);
		bottom = Math.Clamp(bottom, 0, imageHeight);

		return new SKRectI(left, top, right, bottom);
	}
}
