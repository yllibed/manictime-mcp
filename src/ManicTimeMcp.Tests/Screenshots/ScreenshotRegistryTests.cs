using AwesomeAssertions;
using ManicTimeMcp.Screenshots;

namespace ManicTimeMcp.Tests.Screenshots;

[TestClass]
public sealed class ScreenshotRegistryTests
{
	[TestMethod]
	public void Register_ReturnsDeterministicRef()
	{
		var registry = new ScreenshotRegistry();
		var info = CreateInfo("2025-01-15", "08-30-00");

		var ref1 = registry.Register(info);
		var ref2 = registry.Register(info);

		ref1.Should().NotBeNullOrWhiteSpace();
		ref1.Should().Be(ref2);
	}

	[TestMethod]
	public void Register_DifferentScreenshots_DifferentRefs()
	{
		var registry = new ScreenshotRegistry();
		var info1 = CreateInfo("2025-01-15", "08-30-00");
		var info2 = CreateInfo("2025-01-15", "09-00-00");

		var ref1 = registry.Register(info1);
		var ref2 = registry.Register(info2);

		ref1.Should().NotBe(ref2);
	}

	[TestMethod]
	public void TryResolve_RegisteredRef_ReturnsInfo()
	{
		var registry = new ScreenshotRegistry();
		var info = CreateInfo("2025-01-15", "08-30-00");
		var refId = registry.Register(info);

		var resolved = registry.TryResolve(refId);

		resolved.Should().NotBeNull();
		resolved!.Date.Should().Be(info.Date);
		resolved.Time.Should().Be(info.Time);
		resolved.FilePath.Should().Be(info.FilePath);
	}

	[TestMethod]
	public void TryResolve_UnknownRef_ReturnsNull()
	{
		var registry = new ScreenshotRegistry();

		var resolved = registry.TryResolve("nonexistent-ref");

		resolved.Should().BeNull();
	}

	[TestMethod]
	public void ScreenshotRef_Create_UrlSafe()
	{
		var info = CreateInfo("2025-01-15", "08-30-00");

		var refId = ScreenshotRef.Create(info);

		// Should not contain URL-unsafe characters
		refId.Should().NotContain("+");
		refId.Should().NotContain("/");
		refId.Should().NotContain("=");
	}

	[TestMethod]
	public void ScreenshotRef_Create_Deterministic()
	{
		var info1 = CreateInfo("2025-01-15", "08-30-00");
		var info2 = CreateInfo("2025-01-15", "08-30-00");

		ScreenshotRef.Create(info1).Should().Be(ScreenshotRef.Create(info2));
	}

	[TestMethod]
	public void ScreenshotRef_Create_ThumbnailVsFullDiffer()
	{
		var full = CreateInfo("2025-01-15", "08-30-00", isThumbnail: false);
		var thumb = CreateInfo("2025-01-15", "08-30-00", isThumbnail: true);

		ScreenshotRef.Create(full).Should().NotBe(ScreenshotRef.Create(thumb));
	}

	private static ScreenshotInfo CreateInfo(string date, string time, bool isThumbnail = false)
	{
		var d = DateOnly.ParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
		var parts = time.Split('-');
		var t = new TimeOnly(
			int.Parse(parts[0], System.Globalization.CultureInfo.InvariantCulture),
			int.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture),
			int.Parse(parts[2], System.Globalization.CultureInfo.InvariantCulture));

		return new ScreenshotInfo
		{
			Date = d,
			Time = t,
			Offset = "+02-00",
			Width = 1920,
			Height = 1080,
			Sequence = 0,
			Monitor = 0,
			IsThumbnail = isThumbnail,
			FilePath = $"/screenshots/{date}_{time}_+02-00_1920_1080_0_0{(isThumbnail ? ".thumbnail" : "")}.jpg",
		};
	}
}
