using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;
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

	private static McpTestHarness CreateHarness()
	{
		return new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<IDataDirectoryResolver>(new StubDataDirectoryResolver(@"C:\TestData"));
			services.AddSingleton<IHealthService>(new StubHealthService());
			services.AddSingleton<ITimelineRepository>(new StubTimelineRepository(SampleTimelines));
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
}
