using AwesomeAssertions;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.Logging.Abstractions;
using SkiaSharp;

namespace ManicTimeMcp.Tests.Screenshots;

[TestClass]
public sealed class CropServiceTests
{
	private readonly CropService _service = new(NullLogger<CropService>.Instance);

	[TestMethod]
	public void Crop_PercentFullImage_ReturnsSameSize()
	{
		var jpeg = CreateTestJpeg(200, 100);
		var region = new CropRegion { X = 0, Y = 0, Width = 100, Height = 100 };

		var result = _service.Crop(jpeg, region);

		result.Should().NotBeNull();
		var (w, h) = DecodeSize(result!);
		w.Should().Be(200);
		h.Should().Be(100);
	}

	[TestMethod]
	public void Crop_PercentTopLeftQuarter_ReturnsQuarterSize()
	{
		var jpeg = CreateTestJpeg(200, 100);
		var region = new CropRegion { X = 0, Y = 0, Width = 50, Height = 50 };

		var result = _service.Crop(jpeg, region);

		result.Should().NotBeNull();
		var (w, h) = DecodeSize(result!);
		w.Should().Be(100);
		h.Should().Be(50);
	}

	[TestMethod]
	public void Crop_PercentBottomRightQuarter_ReturnsQuarterSize()
	{
		var jpeg = CreateTestJpeg(200, 100);
		var region = new CropRegion { X = 50, Y = 50, Width = 50, Height = 50 };

		var result = _service.Crop(jpeg, region);

		result.Should().NotBeNull();
		var (w, h) = DecodeSize(result!);
		w.Should().Be(100);
		h.Should().Be(50);
	}

	[TestMethod]
	public void Crop_NormalizedCoordinates_TransformsCorrectly()
	{
		var jpeg = CreateTestJpeg(200, 100);
		var region = new CropRegion
		{
			X = 0.25, Y = 0.25, Width = 0.5, Height = 0.5,
			Units = CoordinateUnits.Normalized,
		};

		var result = _service.Crop(jpeg, region);

		result.Should().NotBeNull();
		var (w, h) = DecodeSize(result!);
		w.Should().Be(100); // 0.5 * 200
		h.Should().Be(50);  // 0.5 * 100
	}

	[TestMethod]
	public void Crop_OutOfBoundsRegion_ClampsToBounds()
	{
		var jpeg = CreateTestJpeg(200, 100);
		// Region extends beyond image: x=80%, width=50% -> clamped to right edge
		var region = new CropRegion { X = 80, Y = 80, Width = 50, Height = 50 };

		var result = _service.Crop(jpeg, region);

		result.Should().NotBeNull();
		var (w, h) = DecodeSize(result!);
		// x starts at 80% = px 160, width would extend to 130% but clamped to 100% = px 200
		w.Should().Be(40);
		// y starts at 80% = px 80, height would extend to 130% but clamped to 100% = px 100
		h.Should().Be(20);
	}

	[TestMethod]
	public void Crop_NegativeCoordinates_ClampedToZero()
	{
		var jpeg = CreateTestJpeg(200, 100);
		var region = new CropRegion { X = -10, Y = -10, Width = 50, Height = 50 };

		var result = _service.Crop(jpeg, region);

		result.Should().NotBeNull();
		var (w, h) = DecodeSize(result!);
		// Clamped: x=0, y=0, w=50%=100px, h=50%=50px
		w.Should().Be(100);
		h.Should().Be(50);
	}

	[TestMethod]
	public void Crop_ZeroSizeRegion_ReturnsNull()
	{
		var jpeg = CreateTestJpeg(200, 100);
		var region = new CropRegion { X = 50, Y = 50, Width = 0, Height = 0 };

		var result = _service.Crop(jpeg, region);

		result.Should().BeNull();
	}

	[TestMethod]
	public void Crop_InvalidJpeg_ReturnsNull()
	{
		var badData = new byte[] { 0x00, 0x01, 0x02, 0x03 };
		var region = new CropRegion { X = 0, Y = 0, Width = 50, Height = 50 };

		var result = _service.Crop(badData, region);

		result.Should().BeNull();
	}

	[TestMethod]
	public void Crop_ResultIsValidJpeg()
	{
		var jpeg = CreateTestJpeg(200, 100);
		var region = new CropRegion { X = 10, Y = 10, Width = 30, Height = 30 };

		var result = _service.Crop(jpeg, region);

		result.Should().NotBeNull();
		// JPEG magic bytes
		result![0].Should().Be(0xFF);
		result[1].Should().Be(0xD8);
	}

	[TestMethod]
	public void ToPixelRect_Percent_ComputesCorrectPixels()
	{
		var rect = CropService.ToPixelRect(400, 200, new CropRegion { X = 25, Y = 25, Width = 50, Height = 50 });

		rect.Left.Should().Be(100);
		rect.Top.Should().Be(50);
		rect.Right.Should().Be(300);
		rect.Bottom.Should().Be(150);
	}

	[TestMethod]
	public void ToPixelRect_Normalized_ComputesCorrectPixels()
	{
		var rect = CropService.ToPixelRect(400, 200, new CropRegion
		{
			X = 0.25, Y = 0.25, Width = 0.5, Height = 0.5,
			Units = CoordinateUnits.Normalized,
		});

		rect.Left.Should().Be(100);
		rect.Top.Should().Be(50);
		rect.Right.Should().Be(300);
		rect.Bottom.Should().Be(150);
	}

	[TestMethod]
	public void ToPixelRect_OverflowClamped()
	{
		var rect = CropService.ToPixelRect(100, 100, new CropRegion { X = 90, Y = 90, Width = 50, Height = 50 });

		// Right = min(round((0.9+0.5)*100), 100) = 100
		rect.Right.Should().Be(100);
		rect.Bottom.Should().Be(100);
	}

	private static byte[] CreateTestJpeg(int width, int height)
	{
		using var bitmap = new SKBitmap(width, height);
		using var canvas = new SKCanvas(bitmap);
		canvas.Clear(SKColors.CornflowerBlue);

		// Draw a red rectangle in the center for visual distinctness
		using var paint = new SKPaint { Color = SKColors.Red };
		canvas.DrawRect(width / 4f, height / 4f, width / 2f, height / 2f, paint);

		using var image = SKImage.FromBitmap(bitmap);
		using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
		return data.ToArray();
	}

	private static (int Width, int Height) DecodeSize(byte[] jpeg)
	{
		using var bitmap = SKBitmap.Decode(jpeg);
		return (bitmap.Width, bitmap.Height);
	}
}
