using AwesomeAssertions;
using ManicTimeMcp.Screenshots;

namespace ManicTimeMcp.Tests.Screenshots;

[TestClass]
public sealed class ScreenshotFilenameParserTests
{
	#region Valid filenames

	[TestMethod]
	public void TryParse_FullSize_ParsesAllFields()
	{
		var result = ScreenshotFilenameParser.TryParse(
			@"C:\ManicTime\Screenshots\2025-01-15_08-30-00_+02-00_1920_1080_0_0.jpg");

		result.Should().NotBeNull();
		result!.Date.Should().Be(new DateOnly(2025, 1, 15));
		result.Time.Should().Be(new TimeOnly(8, 30, 0));
		result.Offset.Should().Be("+02-00");
		result.Width.Should().Be(1920);
		result.Height.Should().Be(1080);
		result.Sequence.Should().Be(0);
		result.Monitor.Should().Be(0);
		result.IsThumbnail.Should().BeFalse();
	}

	[TestMethod]
	public void TryParse_Thumbnail_DetectedCorrectly()
	{
		var result = ScreenshotFilenameParser.TryParse(
			@"C:\ManicTime\Screenshots\2025-01-15_08-30-00_+02-00_1920_1080_0_0.thumbnail.jpg");

		result.Should().NotBeNull();
		result!.IsThumbnail.Should().BeTrue();
		result.Width.Should().Be(1920);
	}

	[TestMethod]
	public void TryParse_NegativeOffset_Parsed()
	{
		var result = ScreenshotFilenameParser.TryParse(
			@"C:\Screenshots\2025-06-20_14-05-30_-05-00_2560_1440_1_2.jpg");

		result.Should().NotBeNull();
		result!.Offset.Should().Be("-05-00");
		result.Sequence.Should().Be(1);
		result.Monitor.Should().Be(2);
	}

	[TestMethod]
	public void TryParse_MidnightTimestamp_Parsed()
	{
		var result = ScreenshotFilenameParser.TryParse(
			"2025-12-31_23-59-59_+00-00_800_600_0_0.jpg");

		result.Should().NotBeNull();
		result!.Time.Should().Be(new TimeOnly(23, 59, 59));
	}

	[TestMethod]
	public void TryParse_LocalTimestamp_CombinesDateAndTime()
	{
		var result = ScreenshotFilenameParser.TryParse(
			"2025-03-10_16-45-20_+02-00_1920_1080_0_0.jpg");

		result.Should().NotBeNull();
		result!.LocalTimestamp.Should().Be(new DateTime(2025, 3, 10, 16, 45, 20));
	}

	[TestMethod]
	public void TryParse_FilenameOnly_NoDirectory()
	{
		var result = ScreenshotFilenameParser.TryParse(
			"2025-01-15_08-30-00_+02-00_1920_1080_0_0.jpg");

		result.Should().NotBeNull();
	}


[TestMethod]
public void TryParse_UnsignedPositiveOffset_ParsedAndNormalized()
{
// ManicTime on UTC+ systems (e.g. de-CH, UTC+1) writes offsets without
// the leading '+' sign: "01-00" instead of "+01-00".
var result = ScreenshotFilenameParser.TryParse(
"2026-02-27_06-14-44_01-00_3866_2330_517204_1.jpg");

result.Should().NotBeNull();
result!.Date.Should().Be(new DateOnly(2026, 2, 27));
result.Time.Should().Be(new TimeOnly(6, 14, 44));
result.Offset.Should().Be("+01-00");
result.Width.Should().Be(3866);
result.Height.Should().Be(2330);
result.Sequence.Should().Be(517204);
result.Monitor.Should().Be(1);
result.IsThumbnail.Should().BeFalse();
}

[TestMethod]
public void TryParse_UnsignedPositiveOffset_Thumbnail_ParsedCorrectly()
{
var result = ScreenshotFilenameParser.TryParse(
"2026-02-27_06-14-44_01-00_3866_2330_517204_1.thumbnail.jpg");

result.Should().NotBeNull();
result!.Offset.Should().Be("+01-00");
result.IsThumbnail.Should().BeTrue();
}

[TestMethod]
public void TryParse_UnsignedZeroOffset_ParsedAndNormalized()
{
var result = ScreenshotFilenameParser.TryParse(
"2025-07-01_12-00-00_00-00_1920_1080_0_0.jpg");

result.Should().NotBeNull();
result!.Offset.Should().Be("+00-00");
}
	#endregion

	#region Invalid filenames

	[TestMethod]
	public void TryParse_EmptyString_ReturnsNull()
	{
		ScreenshotFilenameParser.TryParse("").Should().BeNull();
	}

	[TestMethod]
	public void TryParse_NonJpg_ReturnsNull()
	{
		ScreenshotFilenameParser.TryParse("2025-01-15_08-30-00_+02-00_1920_1080_0_0.png").Should().BeNull();
	}

	[TestMethod]
	public void TryParse_TooFewSegments_ReturnsNull()
	{
		ScreenshotFilenameParser.TryParse("2025-01-15_08-30-00.jpg").Should().BeNull();
	}

	[TestMethod]
	public void TryParse_InvalidDate_ReturnsNull()
	{
		ScreenshotFilenameParser.TryParse("2025-13-15_08-30-00_+02-00_1920_1080_0_0.jpg").Should().BeNull();
	}

	[TestMethod]
	public void TryParse_InvalidTime_ReturnsNull()
	{
		ScreenshotFilenameParser.TryParse("2025-01-15_25-30-00_+02-00_1920_1080_0_0.jpg").Should().BeNull();
	}

	[TestMethod]
	public void TryParse_MissingOffset_ReturnsNull()
	{
		ScreenshotFilenameParser.TryParse("2025-01-15_08-30-00_1920_1080_0_0.jpg").Should().BeNull();
	}

	[TestMethod]
	public void TryParse_NegativeWidth_ReturnsNull()
	{
		ScreenshotFilenameParser.TryParse("2025-01-15_08-30-00_+02-00_-1920_1080_0_0.jpg").Should().BeNull();
	}

	[TestMethod]
	public void TryParse_RandomText_ReturnsNull()
	{
		ScreenshotFilenameParser.TryParse("some_random_file.jpg").Should().BeNull();
	}

	[TestMethod]
	public void TryParse_DirectoryOnly_ReturnsNull()
	{
		ScreenshotFilenameParser.TryParse(@"C:\ManicTime\Screenshots\").Should().BeNull();
	}

	#endregion
}
