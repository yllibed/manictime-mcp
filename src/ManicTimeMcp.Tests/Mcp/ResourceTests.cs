using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class ResourceTests
{
	private static readonly TimelineDto[] SampleTimelines =
	[
		new() { ReportId = 1, SchemaName = "ManicTime/Applications", BaseSchemaName = "ManicTime/Applications" },
	];

	private static readonly EnvironmentDto[] SampleEnvironments =
	[
		new() { EnvironmentId = 1, DeviceName = "TEST-PC" },
	];

	private static readonly TimelineSummaryDto[] SampleSummaries =
	[
		new() { ReportId = 1, StartLocalTime = "2025-01-01 00:00:00", EndLocalTime = "2025-01-31 23:59:59" },
	];

	private static McpTestHarness CreateHarness()
	{
		return new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<IDataDirectoryResolver>(new StubDataDirectoryResolver(@"C:\TestData"));
			services.AddSingleton<IHealthService>(new StubHealthService());
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
			services.AddSingleton<IEnvironmentRepository>(new StubEnvironmentRepository(SampleEnvironments));
			services.AddSingleton<IUsageRepository>(new StubUsageRepository(summaries: SampleSummaries));
			services.AddSingleton<IScreenshotRegistry, ScreenshotRegistry>();
			services.AddSingleton<IScreenshotService>(new StubScreenshotService());
			builder.WithResources<ManicTimeResources>();
		});
	}

	[TestMethod]
	public async Task ListResources_ContainsAllExpectedResources()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var resources = await client.ListResourcesAsync().ConfigureAwait(false);

		var uris = resources.Select(r => r.Uri).ToList();
		uris.Should().Contain("manictime://config");
		uris.Should().Contain("manictime://timelines");
		uris.Should().Contain("manictime://health");
		uris.Should().Contain("manictime://guide");
		uris.Should().Contain("manictime://environment");
		uris.Should().Contain("manictime://data-range");
	}

	[TestMethod]
	public async Task ReadConfig_ReturnsDataDirectoryInfo()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.ReadResourceAsync(
			"manictime://config").ConfigureAwait(false);

		result.Contents.Should().ContainSingle();
		var content = result.Contents.OfType<TextResourceContents>().Single();
		var doc = JsonDocument.Parse(content.Text);
		doc.RootElement.GetProperty("dataDirectory").GetString().Should().Be(@"C:\TestData");
	}

	[TestMethod]
	public async Task ReadTimelines_ReturnsTimelineArray()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.ReadResourceAsync(
			"manictime://timelines").ConfigureAwait(false);

		var content = result.Contents.OfType<TextResourceContents>().Single();
		var doc = JsonDocument.Parse(content.Text);
		doc.RootElement.GetArrayLength().Should().Be(1);
		doc.RootElement[0].GetProperty("schemaName").GetString().Should().Be("ManicTime/Applications");
	}

	[TestMethod]
	public async Task ReadHealth_ReturnsHealthReport()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.ReadResourceAsync(
			"manictime://health").ConfigureAwait(false);

		var content = result.Contents.OfType<TextResourceContents>().Single();
		var doc = JsonDocument.Parse(content.Text);
		doc.RootElement.GetProperty("status").GetInt32().Should().Be(0); // HealthStatus.Healthy = 0
	}

	[TestMethod]
	public async Task ReadGuide_ReturnsGuideText()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.ReadResourceAsync(
			"manictime://guide").ConfigureAwait(false);

		var content = result.Contents.OfType<TextResourceContents>().Single();
		content.Text.Should().Contain("ManicTime MCP Usage Guide");
		content.Text.Should().Contain("get_activity_narrative");
	}

	[TestMethod]
	public async Task ReadEnvironment_ReturnsEnvironmentData()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.ReadResourceAsync(
			"manictime://environment").ConfigureAwait(false);

		var content = result.Contents.OfType<TextResourceContents>().Single();
		var doc = JsonDocument.Parse(content.Text);
		doc.RootElement.GetProperty("environments").GetArrayLength().Should().Be(1);
		doc.RootElement.GetProperty("environments")[0].GetProperty("deviceName").GetString().Should().Be("TEST-PC");
	}

	[TestMethod]
	public async Task ReadDataRange_ReturnsTimelineSummaries()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.ReadResourceAsync(
			"manictime://data-range").ConfigureAwait(false);

		var content = result.Contents.OfType<TextResourceContents>().Single();
		var doc = JsonDocument.Parse(content.Text);
		doc.RootElement.GetProperty("timelineSummaries").GetArrayLength().Should().Be(1);
	}

	[TestMethod]
	public async Task ReadScreenshot_UnknownRef_ReturnsErrorText()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.ReadResourceAsync(
			"manictime://screenshot/unknown-ref").ConfigureAwait(false);

		var content = result.Contents.OfType<TextResourceContents>().Single();
		var doc = JsonDocument.Parse(content.Text);
		doc.RootElement.GetProperty("error").GetString().Should().Contain("Unknown");
	}
}
