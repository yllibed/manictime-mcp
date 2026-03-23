using AwesomeAssertions;
using ManicTimeMcp.Repl;

namespace ManicTimeMcp.Tests.Repl;

[TestClass]
public sealed class ManicTimeReplMcpIntegrationTests
{
	[TestMethod]
	public async Task ToolList_ContainsExpectedReplTools()
	{
		await using var harness = await ReplMcpTestHarness.CreateAsync(
			appFactory: static () => ManicTimeReplApp.Create()).ConfigureAwait(false);

		var tools = await harness.Client.ListToolsAsync().ConfigureAwait(false);
		var names = tools.Select(tool => tool.Name).ToList();

		names.Should().Contain("timeline_list");
		names.Should().Contain("activity_list");
		names.Should().Contain("usage_applications");
		names.Should().Contain("summary_daily");
		names.Should().Contain("screenshot_list");
		names.Should().Contain("screenshot_save");
	}

	[TestMethod]
	public async Task ResourceAndPromptDiscovery_ExposeGuideAndDailyReview()
	{
		await using var harness = await ReplMcpTestHarness.CreateAsync(
			appFactory: static () => ManicTimeReplApp.Create()).ConfigureAwait(false);

		var resources = await harness.Client.ListResourcesAsync().ConfigureAwait(false);
		var prompts = await harness.Client.ListPromptsAsync().ConfigureAwait(false);

		resources.Select(resource => resource.Uri).Should().Contain("manictime://resource/guide");
		prompts.Select(prompt => prompt.Name).Should().Contain("prompt_daily-review");
	}

	[TestMethod]
	public async Task PromptGet_ReturnsReplFirstInstructions()
	{
		await using var harness = await ReplMcpTestHarness.CreateAsync(
			appFactory: static () => ManicTimeReplApp.Create()).ConfigureAwait(false);

		var result = await harness.Client.GetPromptAsync(
			name: "prompt_daily-review",
			arguments: new Dictionary<string, object?>(StringComparer.Ordinal)
			{
				["date"] = "2025-01-15",
			}).ConfigureAwait(false);

		result.Messages.Should().ContainSingle();
		var text = result.Messages[0].Content.Should().BeOfType<ModelContextProtocol.Protocol.TextContentBlock>().Which.Text;
		text.Should().Contain("summary narrative");
		text.Should().Contain("2025-01-15");
	}
}
