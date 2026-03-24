using System.Reflection;
using AwesomeAssertions;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class ActivityToolsTests
{
	private static readonly TimelineDto[] SampleTimelines =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
		new() { ReportId = 2, SchemaName = "ManicTime/ComputerUsage", BaseSchemaName = "ManicTime/ComputerUsage" },
		new() { ReportId = 3, SchemaName = "ManicTime/Tags", BaseSchemaName = "ManicTime/Tags" },
		new() { ReportId = 4, SchemaName = "ManicTime/Documents", BaseSchemaName = "ManicTime/Documents" },
	];

	private static readonly ActivityDto[] SampleActivities =
	[
		new() { ActivityId = 1, ReportId = 1, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 09:00:00", Name = "VS Code", GroupId = null },
		new() { ActivityId = 2, ReportId = 1, StartLocalTime = "2025-01-15 09:00:00", EndLocalTime = "2025-01-15 10:00:00", Name = "Chrome", GroupId = null },
		new() { ActivityId = 3, ReportId = 2, StartLocalTime = "2025-01-15 08:00:00", EndLocalTime = "2025-01-15 12:00:00", Name = "On", GroupId = null },
	];

	private static readonly DailyUsageDto[] SampleDailyAppUsage =
	[
		new() { Day = "2025-01-15", Name = "VS Code", Color = "#007ACC", Key = "code.exe", TotalSeconds = 3600 },
		new() { Day = "2025-01-15", Name = "Chrome", Color = "#4285F4", Key = "chrome.exe", TotalSeconds = 1800 },
	];

	private static ActivityTools CreateTools(
		IReadOnlyList<DailyUsageDto>? dailyAppUsage = null,
		IReadOnlyList<DailyUsageDto>? dailyDocUsage = null,
		QueryCapabilityMatrix? capabilities = null) =>
		new(
			new StubActivityRepository(SampleActivities),
			new StubTimelineRepository(SampleTimelines),
			new StubUsageRepository(dailyApp: dailyAppUsage ?? SampleDailyAppUsage, dailyDoc: dailyDocUsage),
			capabilities ?? CreateFullCapabilities());

	[TestMethod]
	public async Task GetActivitiesAsync_ReturnsTimelinePayload()
	{
		var tools = CreateTools();

		var result = await tools.GetActivitiesAsync(1L, "2025-01-15", "2025-01-16", cancellationToken: CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		doc.RootElement.GetProperty("timelineId").GetInt64().Should().Be(1);
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(2);
	}

	[TestMethod]
	public async Task GetActivitiesAsync_InvalidDate_ReturnsError()
	{
		var tools = CreateTools();

		var result = await tools.GetActivitiesAsync(1L, "bad-date", "2025-01-16", cancellationToken: CancellationToken.None).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		result.Payload.Should().Contain("Invalid date format");
	}

	[TestMethod]
	public void GetActivitiesAsync_DescriptionMetadata_ReferencesTimelineList()
	{
		var method = typeof(ActivityTools).GetMethod(nameof(ActivityTools.GetActivitiesAsync));

		method.Should().NotBeNull();
		method!.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()!.Description.Should().Contain("timeline list");
		method.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()!.Description.Should().NotContain("get_timelines");

		var timelineParameter = method.GetParameters().Single(parameter => string.Equals(parameter.Name, "timelineId", StringComparison.Ordinal));
		timelineParameter.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()!.Description.Should().Contain("timeline list");
		timelineParameter.GetCustomAttribute<System.ComponentModel.DescriptionAttribute>()!.Description.Should().NotContain("get_timelines");
	}

	[TestMethod]
	public async Task GetComputerUsageAsync_ReturnsMatchingSchema()
	{
		var tools = CreateTools();

		var result = await tools.GetComputerUsageAsync("2025-01-15", "2025-01-16", cancellationToken: CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		doc.RootElement.GetProperty("schemaName").GetString().Should().Be("ManicTime/ComputerUsage");
		doc.RootElement.GetProperty("count").GetInt32().Should().Be(1);
	}

	[TestMethod]
	public async Task GetApplicationUsageAsync_ReportsFallbackDiagnosticsWhenCapabilityMissing()
	{
		var tools = CreateTools(capabilities: new QueryCapabilityMatrix([]));

		var result = await tools.GetApplicationUsageAsync("2025-01-15", "2025-01-16", cancellationToken: CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		doc.RootElement.GetProperty("diagnostics").GetProperty("degraded").GetBoolean().Should().BeTrue();
	}

	[TestMethod]
	public async Task GetDocumentUsageAsync_ReturnsProjectedMinutes()
	{
		var tools = CreateTools(dailyDocUsage:
		[
			new DailyUsageDto { Day = "2025-01-15", Name = "Program.cs", TotalSeconds = 90 },
		]);

		var result = await tools.GetDocumentUsageAsync("2025-01-15", "2025-01-16", cancellationToken: CancellationToken.None).ConfigureAwait(false);

		var doc = result.ParsePayload();
		doc.RootElement.GetProperty("usage")[0].GetProperty("totalMinutes").GetDouble().Should().Be(1.5);
	}

	private static QueryCapabilityMatrix CreateFullCapabilities() =>
		new(
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
