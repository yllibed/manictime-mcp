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

	[TestMethod]
	public async Task McpSurface_UsesInjectedRepositoriesForTimelineAndActivityQueries()
	{
		var app = CreateApp(services =>
		{
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IActivityRepository>(new StubActivityRepository(SampleActivities));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(dailyApp: SampleDailyAppUsage));
			services.AddSingleton(CreateFullCapabilities());
			services.AddSingleton<IScreenshotRegistry, ScreenshotRegistry>();
			services.AddSingleton<IScreenshotService>(new StubScreenshotService());
		});

		await using var harness = await ReplMcpTestHarness.CreateAsync(() => app).ConfigureAwait(false);

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
		var app = CreateApp(services =>
		{
			services.AddSingleton<IDataDirectoryResolver>(new StubDataDirectoryResolver(@"C:\TestData"));
			services.AddSingleton<IHealthService>(new StubHealthService());
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IEnvironmentRepository>(new StubEnvironmentRepository(SampleEnvironments));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(summaries: SampleSummaries));
			services.AddSingleton<IScreenshotRegistry, ScreenshotRegistry>();
			services.AddSingleton<IScreenshotService>(new StubScreenshotService());
		});

		await using var harness = await ReplMcpTestHarness.CreateAsync(() => app).ConfigureAwait(false);

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
			var clientRoots = new TestMcpClientRoots();
			clientRoots.SetSoftRoots(
			[
				new McpClientRoot(new Uri(rootDirectory + Path.DirectorySeparatorChar), "temp-root"),
			]);

			var app = CreateApp(services =>
			{
				services.AddSingleton<IScreenshotRegistry>(registry);
				services.AddSingleton<IScreenshotService>(new StubScreenshotService(readResult: ScreenshotBytes, writeResult: ScreenshotBytes.Length));
				services.AddSingleton<ICropService>(new StubCropService());
				services.AddSingleton<IMcpClientRoots>(clientRoots);
			});

			await using var harness = await ReplMcpTestHarness.CreateAsync(() => app).ConfigureAwait(false);

			var result = await harness.Client.CallToolAsync(
				"screenshot_save",
				new Dictionary<string, object?>(StringComparer.Ordinal)
				{
					["screenshotRef"] = screenshotRef,
					["outputPath"] = @"assets\focus",
				}).ConfigureAwait(false);

			result.IsError.Should().NotBeTrue();
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

	private static ReplApp CreateApp(Action<IServiceCollection>? configureServices = null) =>
		ManicTimeReplApp.Create(configureServices);

	private static JsonDocument ParseSingleTextPayload(ModelContextProtocol.Protocol.CallToolResult result)
	{
		var text = result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Single().Text;
		return ParseJsonPayload(text);
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
