using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Repl;
using ManicTimeMcp.Screenshots;
using ManicTimeMcp.Tests.Mcp;
using Microsoft.Extensions.DependencyInjection;
using Repl;
using Repl.Mcp;

namespace ManicTimeMcp.Tests.Repl;

[TestClass]
public sealed class ManicTimeReplFeatureTests
{
	private static readonly TimelineDto[] SampleTimelines =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
		new() { ReportId = 2, SchemaName = "ManicTime/ComputerUsage", BaseSchemaName = "ManicTime/ComputerUsage" },
	];

	private static readonly ActivityDto[] SampleActivities =
	[
		new() { ActivityId = 1, ReportId = 1, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:00:00", Name = "VS Code", GroupId = null },
		new() { ActivityId = 2, ReportId = 1, StartLocalTime = "2025-01-15 09:00:00", EndLocalTime = "2025-01-15 10:00:00", Name = "Chrome", GroupId = null },
	];

	private static readonly DailyUsageDto[] SampleDailyAppUsage =
	[
		new() { Day = "2025-01-15", Name = "VS Code", Color = "#007ACC", Key = "code.exe", TotalSeconds = 3600 },
		new() { Day = "2025-01-15", Name = "Chrome", Color = "#4285F4", Key = "chrome.exe", TotalSeconds = 1800 },
	];

	private static readonly DailyUsageDto[] SampleDailyWebUsage =
	[
		new() { Day = "2025-01-15", Name = "github.com", TotalSeconds = 1200 },
	];

	private static readonly EnvironmentDto[] SampleEnvironments =
	[
		new() { EnvironmentId = 1, DeviceName = "TEST-PC" },
	];

	private static readonly TimelineSummaryDto[] SampleSummaries =
	[
		new() { ReportId = 1, StartLocalTime = "2025-01-01 00:00:00", EndLocalTime = "2025-01-31 23:59:59" },
	];

	private static readonly ScreenshotInfo SampleScreenshot = new()
	{
		Date = new DateOnly(2025, 1, 15),
		Time = new TimeOnly(10, 30, 0),
		Offset = "+01-00",
		Width = 1920,
		Height = 1080,
		Sequence = 0,
		Monitor = 0,
		IsThumbnail = false,
		FilePath = @"C:\Data\Screenshots\2025-01-15\2025-01-15_10-30-00_+01-00_1920_1080_0_0.jpg",
	};

	private static readonly byte[] ScreenshotBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];
	private static readonly byte[] ThumbnailBytes = [0x01, 0x02, 0x03, 0x04];
	private static readonly byte[] FullSizeBytes = [0x09, 0x08, 0x07, 0x06, 0x05];

	[TestMethod]
	public async Task McpSurface_UsesInjectedRepositoriesForTimelineAndActivityQueries()
	{
		await using var harness = await CreateHarnessAsync(services =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(SampleActivities));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(dailyApp: SampleDailyAppUsage));
			services.AddSingleton(CreateFullCapabilities());
			services.AddSingleton<IScreenshotRegistry, ScreenshotRegistry>();
			services.AddSingleton<IScreenshotService>(new StubScreenshotService());
		}).ConfigureAwait(false);

		var timelineResult = await harness.Client.CallToolAsync(
			"timeline_list",
			new Dictionary<string, object?>(StringComparer.Ordinal)).ConfigureAwait(false);
		var activityResult = await harness.Client.CallToolAsync(
			"activity_list",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["timelineId"] = 1L,
				["period"] = "2025-01-15..2025-01-16",
			}).ConfigureAwait(false);

		timelineResult.IsError.Should().NotBeTrue();
		activityResult.IsError.Should().NotBeTrue();

		var timelineDoc = ParseSingleTextPayload(timelineResult);
		timelineDoc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
		timelineDoc.RootElement.GetProperty("timelines").GetArrayLength().Should().Be(2);

		var activityDoc = ParseSingleTextPayload(activityResult);
		activityDoc.RootElement.GetProperty("timelineId").GetInt64().Should().Be(1);
		activityDoc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
	}

	[TestMethod]
	public async Task McpResources_ReadInjectedEnvironmentAndDataRange()
	{
		await using var harness = await CreateHarnessAsync(services =>
		{
			services.AddSingleton<IDataDirectoryResolver>(new StubDataDirectoryResolver(@"C:\TestData"));
			services.AddSingleton<IHealthService>(new StubHealthService());
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IEnvironmentRepository>(new StubEnvironmentRepository(SampleEnvironments));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(summaries: SampleSummaries));
			services.AddSingleton<IScreenshotRegistry, ScreenshotRegistry>();
			services.AddSingleton<IScreenshotService>(new StubScreenshotService());
		}).ConfigureAwait(false);

		var configResult = await harness.Client.ReadResourceAsync("manictime://resource/config").ConfigureAwait(false);
		var environmentResult = await harness.Client.ReadResourceAsync("manictime://resource/environment").ConfigureAwait(false);
		var dataRangeResult = await harness.Client.ReadResourceAsync("manictime://resource/data-range").ConfigureAwait(false);

		var configDoc = ParseJsonPayload(configResult.Contents.OfType<ModelContextProtocol.Protocol.TextResourceContents>().Single().Text);
		configDoc.RootElement.GetProperty("dataDirectory").GetString().Should().Be(@"C:\TestData");

		var environmentDoc = ParseJsonPayload(environmentResult.Contents.OfType<ModelContextProtocol.Protocol.TextResourceContents>().Single().Text);
		environmentDoc.RootElement.GetProperty("environments").GetArrayLength().Should().Be(1);

		var dataRangeDoc = ParseJsonPayload(dataRangeResult.Contents.OfType<ModelContextProtocol.Protocol.TextResourceContents>().Single().Text);
		dataRangeDoc.RootElement.GetProperty("timelineSummaries").GetArrayLength().Should().Be(1);
	}

	[TestMethod]
	public async Task ScreenshotSave_UsesSoftRootsThroughReplMcp()
	{
		var registry = new ScreenshotRegistry();
		var screenshotRef = registry.Register(SampleScreenshot);
		var rootDirectory = Path.Combine(Path.GetTempPath(), $"manictime-mcp-roots-{Guid.NewGuid():N}");
		Directory.CreateDirectory(rootDirectory);

		try
		{
			await using var harness = await CreateHarnessAsync(services =>
			{
				services.AddSingleton<IScreenshotRegistry>(registry);
				services.AddSingleton<IScreenshotService>(new StubScreenshotService(readResult: ScreenshotBytes, writeResult: ScreenshotBytes.Length));
				services.AddSingleton<ICropService>(new StubCropService());
			}).ConfigureAwait(false);

			var initResult = await harness.Client.CallToolAsync(
				"workspace_init",
				new Dictionary<string, object?>(StringComparer.Ordinal)
				{
					["path"] = rootDirectory,
				}).ConfigureAwait(false);

			initResult.IsError.Should().NotBeTrue(because: DescribeCallResult(initResult));

			var result = await harness.Client.CallToolAsync(
				"screenshot_save",
				new Dictionary<string, object?>(StringComparer.Ordinal)
				{
					["screenshotRef"] = screenshotRef,
					["outputPath"] = @"assets\focus",
				}).ConfigureAwait(false);

			result.IsError.Should().NotBeTrue(because: DescribeCallResult(result));
			var doc = ParseSingleTextPayload(result);
			doc.RootElement.GetProperty("path").GetString().Should().StartWith(rootDirectory);
			doc.RootElement.GetProperty("path").GetString().Should().EndWith("focus.jpg");
			doc.RootElement.GetProperty("size").GetInt64().Should().Be(ScreenshotBytes.Length);
		}
		finally
		{
			if (Directory.Exists(rootDirectory))
			{
				Directory.Delete(rootDirectory, recursive: true);
			}
		}
	}

	[TestMethod]
	public async Task ScreenshotList_RegistersRefsAndExposesContractFields()
	{
		var registry = new ScreenshotRegistry();
		var screenshotDirectory = Path.Combine(Path.GetTempPath(), $"manictime-mcp-screenshots-{Guid.NewGuid():N}");
		Directory.CreateDirectory(screenshotDirectory);

		var fullSizePath = Path.Combine(screenshotDirectory, "2025-01-15_10-30-00_+01-00_1920_1080_0_0.jpg");
		var thumbnailPath = Path.Combine(screenshotDirectory, "2025-01-15_10-30-00_+01-00_1920_1080_0_0.thumbnail.jpg");
		File.WriteAllBytes(fullSizePath, [0x01]);
		File.WriteAllBytes(thumbnailPath, [0x02]);

		var fullSizeScreenshot = SampleScreenshot with
		{
			IsThumbnail = false,
			FilePath = fullSizePath,
			Ref = null,
		};

		try
		{
			await using var harness = await CreateHarnessAsync(services =>
			{
				services.AddSingleton<IScreenshotRegistry>(registry);
				services.AddSingleton<IScreenshotService>(new StubScreenshotService(
					new ScreenshotSelection
					{
						Screenshots = [fullSizeScreenshot],
						TotalMatching = 1,
						IsTruncated = false,
						SamplingStrategyUsed = SamplingStrategy.Interval,
					}));
			}).ConfigureAwait(false);

			var result = await harness.Client.CallToolAsync(
				"screenshot_list",
				new Dictionary<string, object?>(StringComparer.Ordinal)
				{
					["window"] = "2025-01-15T10:00:00..2025-01-15T11:00:00",
				}).ConfigureAwait(false);

			result.IsError.Should().NotBeTrue();

			var doc = ParseSingleTextPayload(result);
			var screenshot = doc.RootElement.GetProperty("screenshots")[0];
			screenshot.GetProperty("screenshotRef").GetString().Should().NotBeNullOrWhiteSpace();
			screenshot.GetProperty("displayLocalTime").GetString().Should().Be("2025-01-15 10:30:00");
			screenshot.GetProperty("hasThumbnail").GetBoolean().Should().BeTrue();
			screenshot.TryGetProperty("resourceUri", out _).Should().BeFalse();
			screenshot.TryGetProperty("isThumbnail", out _).Should().BeFalse();
		}
		finally
		{
			if (Directory.Exists(screenshotDirectory))
			{
				Directory.Delete(screenshotDirectory, recursive: true);
			}
		}
	}

	[TestMethod]
	public async Task ScreenshotGet_PrefersThumbnailPayloadWhenAvailable()
	{
		var registry = new ScreenshotRegistry();
		var screenshotRef = registry.Register(SampleScreenshot);
		var readPaths = new List<string>();
		await using var harness = await CreateHarnessAsync(services =>
		{
			services.AddSingleton<IScreenshotRegistry>(registry);
			services.AddSingleton<IScreenshotService>(new StubScreenshotService(
				onReadScreenshot: readPaths.Add,
				readScreenshot: static filePath => filePath.Contains(".thumbnail.", StringComparison.OrdinalIgnoreCase)
					? ThumbnailBytes
					: FullSizeBytes));
		}).ConfigureAwait(false);

		var result = await harness.Client.CallToolAsync(
			"screenshot_get",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["screenshotRef"] = screenshotRef,
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();

		var doc = ParseSingleTextPayload(result);
		doc.RootElement.GetProperty("thumbnailBase64").GetString().Should().Be(Convert.ToBase64String(ThumbnailBytes));
		doc.RootElement.GetProperty("imageBase64").GetString().Should().Be(Convert.ToBase64String(ThumbnailBytes));
		readPaths.Should().ContainSingle(path => path.Contains(".thumbnail.", StringComparison.OrdinalIgnoreCase));
		readPaths.Should().NotContain(path => !path.Contains(".thumbnail.", StringComparison.OrdinalIgnoreCase));
	}

	[TestMethod]
	public async Task ScreenshotSave_PartialCropOptions_ReturnsValidationError()
	{
		var registry = new ScreenshotRegistry();
		var screenshotRef = registry.Register(SampleScreenshot);
		await using var harness = await CreateHarnessAsync(services =>
		{
			services.AddSingleton<IScreenshotRegistry>(registry);
			services.AddSingleton<IScreenshotService>(new StubScreenshotService(readResult: ScreenshotBytes, writeResult: ScreenshotBytes.Length));
			services.AddSingleton<ICropService>(new StubCropService());
		}).ConfigureAwait(false);

		var result = await harness.Client.CallToolAsync(
			"screenshot_save",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["screenshotRef"] = screenshotRef,
				["cropX"] = 10d,
				["cropY"] = 15d,
				["cropWidth"] = 25d,
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		var doc = ParseSingleTextPayload(result);
		doc.RootElement.GetProperty("kind").GetString().Should().Be("validation");
		doc.RootElement.GetProperty("message").GetString().Should().Contain("Provide either all crop values");
	}

	[TestMethod]
	public async Task ScreenshotSave_WithoutRoots_ReturnsMcpRootsRequired()
	{
		var registry = new ScreenshotRegistry();
		var screenshotRef = registry.Register(SampleScreenshot);
		await using var harness = await CreateHarnessAsync(services =>
		{
			services.AddSingleton<IScreenshotRegistry>(registry);
			services.AddSingleton<IScreenshotService>(new StubScreenshotService(readResult: ScreenshotBytes, writeResult: ScreenshotBytes.Length));
			services.AddSingleton<ICropService>(new StubCropService());
		}).ConfigureAwait(false);

		var result = await harness.Client.CallToolAsync(
			"screenshot_save",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["screenshotRef"] = screenshotRef,
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		var doc = ParseSingleTextPayload(result);
		doc.RootElement.GetProperty("code").GetString().Should().Be("mcp_roots_required");
	}

	private static ReplApp CreateApp(Action<IServiceCollection>? configureServices = null) =>
		ManicTimeReplApp.Create(configureServices);

	private static Task<ReplMcpTestHarness> CreateHarnessAsync(Action<IServiceCollection>? configureServices = null) =>
		ReplMcpTestHarness.CreateAsync(() => CreateApp(configureServices));

	private static JsonDocument ParseSingleTextPayload(ModelContextProtocol.Protocol.CallToolResult result)
	{
		var text = result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Single().Text;
		return ParseJsonPayload(text);
	}

	private static string DescribeCallResult(ModelContextProtocol.Protocol.CallToolResult result)
	{
		var payload = string.Join(
			Environment.NewLine,
			result.Content
				.OfType<ModelContextProtocol.Protocol.TextContentBlock>()
				.Select(static block => block.Text));
		return string.IsNullOrWhiteSpace(payload) ? "<no text payload>" : payload;
	}

	private static JsonDocument ParseJsonPayload(string text)
	{
		var parsed = JsonDocument.Parse(text);
		if (parsed.RootElement.ValueKind is not JsonValueKind.String)
		{
			return parsed;
		}

		var nested = parsed.RootElement.GetString();
		parsed.Dispose();
		return JsonDocument.Parse(nested!);
	}

	private static QueryCapabilityMatrix CreateFullCapabilities()
	{
		return new QueryCapabilityMatrix(
		[
			"Ar_CommonGroup",
			"Ar_ApplicationByDay",
			"Ar_WebSiteByDay",
			"Ar_DocumentByDay",
			"Ar_ApplicationByYear",
			"Ar_WebSiteByYear",
			"Ar_DocumentByYear",
			"Ar_ActivityByHour",
			"Ar_TimelineSummary",
			"Ar_Environment",
			"Ar_Folder",
			"Ar_Tag",
			"Ar_ActivityTag",
			"Ar_Category",
			"Ar_CategoryGroup",
		]);
	}
}
