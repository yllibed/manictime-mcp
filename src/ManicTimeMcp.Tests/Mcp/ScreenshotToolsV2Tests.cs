using System.Text.Json;
using AwesomeAssertions;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Protocol;

namespace ManicTimeMcp.Tests.Mcp;

[TestClass]
public sealed class ScreenshotToolsV2Tests
{
	private static readonly ScreenshotInfo SampleThumbnail = new()
	{
		Date = new DateOnly(2025, 1, 15),
		Time = new TimeOnly(10, 30, 0),
		Offset = "+01-00",
		Width = 320,
		Height = 180,
		Sequence = 0,
		Monitor = 0,
		IsThumbnail = true,
		FilePath = @"C:\Data\Screenshots\2025-01-15\2025-01-15_10-30-00_+01-00_320_180_0_0.thumbnail.jpg",
	};

	private static readonly ScreenshotInfo SampleFullSize = new()
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

	/// <summary>Minimal valid JPEG bytes (SOI + EOI markers).</summary>
	private static readonly byte[] ThumbnailBytes = [0xFF, 0xD8, 0xFF, 0xD9];
	private static readonly byte[] FullSizeBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

	private static McpTestHarness CreateHarness(
		ScreenshotSelection? selection = null,
		byte[]? thumbnailBytes = null,
		byte[]? fullBytes = null)
	{
		var screenshotService = new StubScreenshotService(
			selection,
			readResult: thumbnailBytes ?? fullBytes);

		return new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<IScreenshotService>(screenshotService);
			services.AddSingleton<IScreenshotRegistry, ScreenshotRegistry>();
			services.AddSingleton<ICropService>(new StubCropService());
			builder.WithTools<ScreenshotToolsV2>();
		});
	}

	[TestMethod]
	public async Task ListTools_ContainsProgressiveScreenshotTools()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var tools = await client.ListToolsAsync().ConfigureAwait(false);
		var toolNames = tools.Select(t => t.Name).ToList();

		toolNames.Should().Contain("list_screenshots");
		toolNames.Should().Contain("get_screenshot");
		toolNames.Should().Contain("crop_screenshot");
	}

	[TestMethod]
	public async Task ListScreenshots_EmptyResult_ReturnsEmptyArray()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"list_screenshots",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();
		var textBlock = result.Content.OfType<TextContentBlock>().First();
		var doc = JsonDocument.Parse(textBlock.Text);
		doc.RootElement.GetProperty("screenshots").GetArrayLength().Should().Be(0);
	}

	[TestMethod]
	public async Task ListScreenshots_WithData_ReturnsMetadataAndResourceLinks()
	{
		var registered = SampleThumbnail with { Ref = "test-ref" };
		var selection = new ScreenshotSelection
		{
			Screenshots = [registered],
			TotalMatching = 1,
			IsTruncated = false,
			SamplingStrategyUsed = SamplingStrategy.ActivityTransition,
		};

		await using var harness = CreateHarness(selection);
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"list_screenshots",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();

		// Should have TextContentBlock (metadata) + ResourceLinkBlock(s)
		var textBlocks = result.Content.OfType<TextContentBlock>().ToList();
		textBlocks.Should().HaveCount(1);

		var doc = JsonDocument.Parse(textBlocks[0].Text);
		var screenshots = doc.RootElement.GetProperty("screenshots");
		screenshots.GetArrayLength().Should().Be(1);
		screenshots[0].GetProperty("screenshotRef").GetString().Should().NotBeNullOrEmpty();
		screenshots[0].GetProperty("resourceUri").GetString().Should().StartWith("manictime://screenshot/");

		var resourceLinks = result.Content.OfType<ResourceLinkBlock>().ToList();
		resourceLinks.Should().HaveCount(1);
		resourceLinks[0].Uri.Should().StartWith("manictime://screenshot/");
		resourceLinks[0].Name.Should().Contain("2025-01-15");
		resourceLinks[0].MimeType.Should().Be("image/jpeg");
	}

	[TestMethod]
	public async Task ListScreenshots_ZeroImageBytes()
	{
		var selection = new ScreenshotSelection
		{
			Screenshots = [SampleThumbnail with { Ref = "test-ref" }],
			TotalMatching = 1,
			IsTruncated = false,
		};

		await using var harness = CreateHarness(selection);
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"list_screenshots",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "2025-01-15",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		// list_screenshots should never return ImageContentBlock
		result.Content.OfType<ImageContentBlock>().Should().BeEmpty();
	}

	[TestMethod]
	public async Task GetScreenshot_UnknownRef_ReturnsError()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_screenshot",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["screenshotRef"] = "nonexistent",
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		text.Should().Contain("Unknown screenshotRef");
	}

	[TestMethod]
	public async Task GetScreenshot_WithRegisteredRef_ReturnsImageContentBlocks()
	{
		// Pre-register a screenshot in the registry
		var registry = new ScreenshotRegistry();
		var refId = registry.Register(SampleThumbnail);

		var screenshotService = new StubScreenshotService(readResult: ThumbnailBytes);

		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<IScreenshotService>(screenshotService);
			services.AddSingleton<IScreenshotRegistry>(registry);
			services.AddSingleton<ICropService>(new StubCropService());
			builder.WithTools<ScreenshotToolsV2>();
		});

		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"get_screenshot",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["screenshotRef"] = refId,
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();

		// Should have at least one ImageContentBlock and one TextContentBlock
		var images = result.Content.OfType<ImageContentBlock>().ToList();
		images.Should().NotBeEmpty();
		images[0].MimeType.Should().Be("image/jpeg");
		images[0].Annotations.Should().NotBeNull();
		var audience = images[0].Annotations!.Audience!;
		audience.Should().Contain(Role.User);
		audience.Should().Contain(Role.Assistant);

		var textBlocks = result.Content.OfType<TextContentBlock>().ToList();
		textBlocks.Should().HaveCount(1);
	}

	[TestMethod]
	public async Task CropScreenshot_UnknownRef_ReturnsError()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"crop_screenshot",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["screenshotRef"] = "nonexistent",
				["x"] = 10,
				["y"] = 10,
				["width"] = 50,
				["height"] = 50,
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		text.Should().Contain("Unknown screenshotRef");
	}

	[TestMethod]
	public async Task CropScreenshot_WithRegisteredRef_ReturnsImageAndMetadata()
	{
		var registry = new ScreenshotRegistry();
		var refId = registry.Register(SampleFullSize);
		var cropResult = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };

		var screenshotService = new StubScreenshotService(readResult: FullSizeBytes);

		await using var harness = new McpTestHarness((services, builder) =>
		{
			services.AddSingleton<IScreenshotService>(screenshotService);
			services.AddSingleton<IScreenshotRegistry>(registry);
			services.AddSingleton<ICropService>(new StubCropService(cropResult));
			builder.WithTools<ScreenshotToolsV2>();
		});

		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"crop_screenshot",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["screenshotRef"] = refId,
				["x"] = 10,
				["y"] = 20,
				["width"] = 50,
				["height"] = 50,
			}).ConfigureAwait(false);

		result.IsError.Should().NotBeTrue();

		var images = result.Content.OfType<ImageContentBlock>().ToList();
		images.Should().HaveCount(1);
		images[0].MimeType.Should().Be("image/jpeg");
		images[0].Annotations.Should().NotBeNull();
		var cropAudience = images[0].Annotations!.Audience!;
		cropAudience.Should().Contain(Role.User);
		cropAudience.Should().Contain(Role.Assistant);

		var textBlocks = result.Content.OfType<TextContentBlock>().ToList();
		textBlocks.Should().HaveCount(1);
		var doc = JsonDocument.Parse(textBlocks[0].Text);
		doc.RootElement.GetProperty("crop").GetProperty("x").GetDouble().Should().Be(10);
	}

	[TestMethod]
	public async Task ListScreenshots_InvalidDate_ReturnsError()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"list_screenshots",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["startDate"] = "not-a-date",
				["endDate"] = "2025-01-16",
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		text.Should().Contain("Invalid date format");
	}

	[TestMethod]
	public async Task ListTools_ContainsSaveScreenshotTool()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var tools = await client.ListToolsAsync().ConfigureAwait(false);
		tools.Select(t => t.Name).Should().Contain("save_screenshot");
	}

	[TestMethod]
	public async Task SaveScreenshot_UnknownRef_ReturnsError()
	{
		await using var harness = CreateHarness();
		await using var client = await harness.CreateClientAsync().ConfigureAwait(false);
		var result = await client.CallToolAsync(
			"save_screenshot",
			new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["screenshotRef"] = "nonexistent",
			}).ConfigureAwait(false);

		result.IsError.Should().BeTrue();
		var text = result.Content.OfType<TextContentBlock>().Single().Text;
		text.Should().Contain("Unknown screenshotRef");
	}

	[TestMethod]
	public void RootUriToLocalPath_ValidFileUri_ReturnsLocalPath()
	{
		var path = ScreenshotToolsV2.RootUriToLocalPath("file:///C:/Users/test/project");
		path.Should().NotBeNull();
		path.Should().Contain("Users");
	}

	[TestMethod]
	public void RootUriToLocalPath_NonFileUri_ReturnsNull()
	{
		ScreenshotToolsV2.RootUriToLocalPath("https://example.com").Should().BeNull();
	}

	[TestMethod]
	public void RootUriToLocalPath_InvalidUri_ReturnsNull()
	{
		ScreenshotToolsV2.RootUriToLocalPath("not a uri").Should().BeNull();
	}
}
