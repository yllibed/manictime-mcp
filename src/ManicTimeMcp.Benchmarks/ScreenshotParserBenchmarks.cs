using BenchmarkDotNet.Attributes;
using ManicTimeMcp.Screenshots;

namespace ManicTimeMcp.Benchmarks;

/// <summary>Benchmarks for screenshot filename parsing hot path.</summary>
[MemoryDiagnoser]
public class ScreenshotParserBenchmarks
{
	private string _canonicalPath = null!;
	private string _thumbnailPath = null!;
	private string _malformedPath = null!;

	[GlobalSetup]
	public void Setup()
	{
		_canonicalPath = @"C:\Data\Screenshots\2025-01-15\2025-01-15_08-30-00_+02-00_1920_1080_0_0.jpg";
		_thumbnailPath = @"C:\Data\Screenshots\2025-01-15\2025-01-15_08-30-00_+02-00_1920_1080_0_0.thumbnail.jpg";
		_malformedPath = @"C:\Data\Screenshots\random_image.jpg";
	}

	[Benchmark(Baseline = true)]
	public ScreenshotInfo? ParseCanonical() => ScreenshotFilenameParser.TryParse(_canonicalPath);

	[Benchmark]
	public ScreenshotInfo? ParseThumbnail() => ScreenshotFilenameParser.TryParse(_thumbnailPath);

	[Benchmark]
	public ScreenshotInfo? ParseMalformed() => ScreenshotFilenameParser.TryParse(_malformedPath);
}
