using AwesomeAssertions;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManicTimeMcp.Tests.Screenshots;

[TestClass]
public sealed class ScreenshotServiceTests : IDisposable
{
	private readonly string _tempDir;
	private readonly string _screenshotDir;

	public ScreenshotServiceTests()
	{
		_tempDir = Path.Combine(Path.GetTempPath(), $"ManicTimeMcp_SS_{Guid.NewGuid():N}");
		_screenshotDir = Path.Combine(_tempDir, "Screenshots");
		Directory.CreateDirectory(_screenshotDir);
	}

	public void Dispose()
	{
		try
		{
			if (Directory.Exists(_tempDir))
			{
				Directory.Delete(_tempDir, recursive: true);
			}
		}
#pragma warning disable CA1031 // Do not catch general exception types — cleanup best effort
		catch (Exception)
#pragma warning restore CA1031
		{
			// Best effort cleanup
		}
	}

	#region Select — empty/missing directory

	[TestMethod]
	public void Select_MissingDirectory_ReturnsEmpty()
	{
		var resolver = new StubResolver(path: null);
		var sut = CreateService(resolver);

		var result = sut.Select(DefaultQuery());

		result.Screenshots.Should().BeEmpty();
		result.TotalMatching.Should().Be(0);
		result.IsTruncated.Should().BeFalse();
	}

	[TestMethod]
	public void Select_EmptyDirectory_ReturnsEmpty()
	{
		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var result = sut.Select(DefaultQuery());

		result.Screenshots.Should().BeEmpty();
	}

	#endregion

	#region Select — basic selection

	[TestMethod]
	public void Select_MatchingFiles_ReturnsFiltered()
	{
		CreateScreenshotFile("2025-01-15_08-30-00_+02-00_1920_1080_0_0.jpg");
		CreateScreenshotFile("2025-01-15_10-00-00_+02-00_1920_1080_0_0.jpg");
		CreateScreenshotFile("2025-01-16_08-00-00_+02-00_1920_1080_0_0.jpg"); // outside range

		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var result = sut.Select(new ScreenshotQuery
		{
			StartLocalTime = new DateTime(2025, 1, 15, 0, 0, 0),
			EndLocalTime = new DateTime(2025, 1, 16, 0, 0, 0),
			PreferThumbnails = false,
		});

		result.Screenshots.Count.Should().Be(2);
		result.TotalMatching.Should().Be(2);
		result.IsTruncated.Should().BeFalse();
	}

	#endregion

	#region Select — thumbnail preference

	[TestMethod]
	public void Select_PreferThumbnails_SelectsThumbnailWhenAvailable()
	{
		CreateScreenshotFile("2025-01-15_08-30-00_+02-00_1920_1080_0_0.jpg");
		CreateScreenshotFile("2025-01-15_08-30-00_+02-00_1920_1080_0_0.thumbnail.jpg");

		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var result = sut.Select(new ScreenshotQuery
		{
			StartLocalTime = new DateTime(2025, 1, 15, 0, 0, 0),
			EndLocalTime = new DateTime(2025, 1, 16, 0, 0, 0),
			PreferThumbnails = true,
		});

		result.Screenshots.Should().ContainSingle().Which.IsThumbnail.Should().BeTrue();
	}

	[TestMethod]
	public void Select_PreferFullSize_SelectsFullWhenAvailable()
	{
		CreateScreenshotFile("2025-01-15_08-30-00_+02-00_1920_1080_0_0.jpg");
		CreateScreenshotFile("2025-01-15_08-30-00_+02-00_1920_1080_0_0.thumbnail.jpg");

		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var result = sut.Select(new ScreenshotQuery
		{
			StartLocalTime = new DateTime(2025, 1, 15, 0, 0, 0),
			EndLocalTime = new DateTime(2025, 1, 16, 0, 0, 0),
			PreferThumbnails = false,
		});

		result.Screenshots.Should().ContainSingle().Which.IsThumbnail.Should().BeFalse();
	}

	#endregion

	#region Select — sampling

	[TestMethod]
	public void Select_SamplingInterval_ReducesResults()
	{
		// Create screenshots every 5 minutes from 08:00 to 08:55 (12 screenshots)
		for (var min = 0; min < 60; min += 5)
		{
			CreateScreenshotFile($"2025-01-15_08-{min:D2}-00_+02-00_1920_1080_0_0.jpg");
		}

		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var result = sut.Select(new ScreenshotQuery
		{
			StartLocalTime = new DateTime(2025, 1, 15, 8, 0, 0),
			EndLocalTime = new DateTime(2025, 1, 15, 9, 0, 0),
			SamplingInterval = TimeSpan.FromMinutes(15),
			PreferThumbnails = false,
		});

		// With 15-min sampling: 08:00, 08:15, 08:30, 08:45 = 4
		result.Screenshots.Count.Should().Be(4);
	}

	#endregion

	#region Select — limits

	[TestMethod]
	public void Select_ExplicitLimit_Truncates()
	{
		for (var i = 0; i < 10; i++)
		{
			CreateScreenshotFile($"2025-01-15_08-{i:D2}-00_+02-00_1920_1080_0_0.jpg");
		}

		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var result = sut.Select(new ScreenshotQuery
		{
			StartLocalTime = new DateTime(2025, 1, 15, 0, 0, 0),
			EndLocalTime = new DateTime(2025, 1, 16, 0, 0, 0),
			MaxCount = 3,
			PreferThumbnails = false,
		});

		result.Screenshots.Count.Should().Be(3);
		result.IsTruncated.Should().BeTrue();
	}

	#endregion

	#region ReadScreenshot — security

	[TestMethod]
	public void ReadScreenshot_ValidFile_ReturnsBytes()
	{
		var filePath = CreateScreenshotFile("2025-01-15_08-30-00_+02-00_1920_1080_0_0.jpg");
		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var bytes = sut.ReadScreenshot(filePath);

		bytes.Should().NotBeNull();
		bytes!.Length.Should().BeGreaterThan(0);
	}

	[TestMethod]
	public void ReadScreenshot_PathTraversal_ReturnsNull()
	{
		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var bytes = sut.ReadScreenshot(Path.Combine(_screenshotDir, "..", "..", "etc", "passwd"));

		bytes.Should().BeNull();
	}

	[TestMethod]
	public void ReadScreenshot_NonJpgExtension_ReturnsNull()
	{
		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var txtPath = Path.Combine(_screenshotDir, "test.txt");
		File.WriteAllText(txtPath, "not a jpg");

		var bytes = sut.ReadScreenshot(txtPath);

		bytes.Should().BeNull();
	}

	[TestMethod]
	public void ReadScreenshot_NonexistentFile_ReturnsNull()
	{
		var resolver = new StubResolver(_tempDir);
		var sut = CreateService(resolver);

		var bytes = sut.ReadScreenshot(Path.Combine(_screenshotDir, "nonexistent.jpg"));

		bytes.Should().BeNull();
	}

	[TestMethod]
	public void ReadScreenshot_NullDataDirectory_ReturnsNull()
	{
		var resolver = new StubResolver(path: null);
		var sut = CreateService(resolver);

		var bytes = sut.ReadScreenshot(@"C:\any\path.jpg");

		bytes.Should().BeNull();
	}

	#endregion

	#region Helpers

	private string CreateScreenshotFile(string fileName)
	{
		var filePath = Path.Combine(_screenshotDir, fileName);
		File.WriteAllBytes(filePath, [0xFF, 0xD8, 0xFF, 0xE0]); // Minimal JPEG header
		return filePath;
	}

	private static ScreenshotQuery DefaultQuery() => new()
	{
		StartLocalTime = new DateTime(2025, 1, 15, 0, 0, 0),
		EndLocalTime = new DateTime(2025, 1, 16, 0, 0, 0),
	};

	private static ScreenshotService CreateService(StubResolver resolver) =>
		new(resolver, NullLogger<ScreenshotService>.Instance);

	private sealed class StubResolver(string? path) : IDataDirectoryResolver
	{
		public DataDirectoryResult Resolve() => new()
		{
			Path = path,
			Source = path is not null ? Models.DataDirectorySource.EnvironmentVariable : Models.DataDirectorySource.Unresolved,
		};
	}

	#endregion
}
