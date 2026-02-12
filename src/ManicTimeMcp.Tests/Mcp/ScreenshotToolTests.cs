using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class ScreenshotToolTests
{
	[TestMethod]
	public async Task ListTools_ContainsGetScreenshots()
	{
		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<IScreenshotService>(new StubScreenshotService());
			builder.WithTools<ScreenshotTools>();
		});

		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var tools = await client.ListToolsAsync().ConfigureAwait(false);

		tools.Should().Contain(t => t.Name == "get_screenshots");
	}

	[TestMethod]
	public async Task GetScreenshots_EmptyResult_ReturnsZeroCount()
	{
		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<IScreenshotService>(new StubScreenshotService());
			builder.WithTools<ScreenshotTools>();
		});

		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_screenshots",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("totalMatching").GetInt32().Should().Be(0);
		doc.RootElement.GetProperty("returned").GetInt32().Should().Be(0);
	}

	[TestMethod]
	public async Task GetScreenshots_WithData_ReturnsBase64()
	{
		var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
		var selection = new ScreenshotSelection
		{
			Screenshots =
			[
				new ScreenshotInfo
				{
					Date = new DateOnly(2025, 1, 15),
					Time = new TimeOnly(10, 30, 0),
					Offset = "+01-00",
					Width = 1920,
					Height = 1080,
					Sequence = 0,
					Monitor = 0,
					IsThumbnail = true,
					FilePath = @"C:\Data\Screenshots\2025-01-15\img.thumbnail.jpg",
				},
			],
			TotalMatching = 1,
			IsTruncated = false,
		};

		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<IScreenshotService>(new StubScreenshotService(selection, imageBytes));
			builder.WithTools<ScreenshotTools>();
		});

		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_screenshots",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		var doc = JsonDocument.Parse(text);
		doc.RootElement.GetProperty("returned").GetInt32().Should().Be(1);

		var screenshot = doc.RootElement.GetProperty("screenshots")[0];
		screenshot.GetProperty("isThumbnail").GetBoolean().Should().BeTrue();
		screenshot.GetProperty("hasData").GetBoolean().Should().BeTrue();
		screenshot.GetProperty("dataBase64").GetString().Should().Be(Convert.ToBase64String(imageBytes));
	}
}
