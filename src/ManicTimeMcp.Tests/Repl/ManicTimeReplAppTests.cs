using AwesomeAssertions;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Repl;
using Microsoft.Extensions.DependencyInjection;
using Repl.Testing;

namespace ManicTimeMcp.Tests.Repl;

[TestClass]
public sealed class ManicTimeReplAppTests
{
	[TestMethod]
	public void DocumentationModel_ContainsExpectedContextsAndMetadata()
	{
		var app = ManicTimeReplApp.Create();
		var model = app.CreateDocumentationModel();

		model.Contexts.Should().Contain(context => string.Equals(context.Path, "timeline", StringComparison.Ordinal));
		model.Contexts.Should().Contain(context => string.Equals(context.Path, "activity", StringComparison.Ordinal));
		model.Contexts.Should().Contain(context => string.Equals(context.Path, "usage", StringComparison.Ordinal));
		model.Contexts.Should().Contain(context => string.Equals(context.Path, "summary", StringComparison.Ordinal));
		model.Contexts.Should().Contain(context => string.Equals(context.Path, "screenshot", StringComparison.Ordinal));
		model.Contexts.Should().Contain(context => string.Equals(context.Path, "resource", StringComparison.Ordinal));
		model.Contexts.Should().Contain(context => string.Equals(context.Path, "prompt", StringComparison.Ordinal));

		model.Commands.Should().NotContain(command => string.Equals(command.Path, "mcp serve", StringComparison.Ordinal));

		var summaryNarrative = model.Commands.Single(command => string.Equals(command.Path, "summary narrative", StringComparison.Ordinal));
		summaryNarrative.Annotations?.ReadOnly.Should().BeTrue();
		summaryNarrative.Options.Should().Contain(option =>
			string.Equals(option.Name, "period", StringComparison.Ordinal)
			&& string.Equals(option.Type, "date-range", StringComparison.OrdinalIgnoreCase));

		var screenshotList = model.Commands.Single(command => string.Equals(command.Path, "screenshot list", StringComparison.Ordinal));
		screenshotList.Options.Should().Contain(option =>
			string.Equals(option.Name, "window", StringComparison.Ordinal)
			&& string.Equals(option.Type, "datetime-range", StringComparison.OrdinalIgnoreCase));

		var screenshotSave = model.Commands.Single(command => string.Equals(command.Path, "screenshot save", StringComparison.Ordinal));
		screenshotSave.Annotations?.OpenWorld.Should().BeTrue();
		screenshotSave.Options.Should().Contain(option => option.Aliases.Contains("--outputPath", StringComparer.Ordinal));
		screenshotSave.Options.Should().Contain(option => option.Aliases.Contains("--cropX", StringComparer.Ordinal));

		var resourceGuide = model.Commands.Single(command => string.Equals(command.Path, "resource guide", StringComparison.Ordinal));
		resourceGuide.IsResource.Should().BeTrue();
		resourceGuide.Annotations?.ReadOnly.Should().BeTrue();

		var promptDailyReview = model.Commands.Single(command => string.Equals(command.Path, "prompt daily-review", StringComparison.Ordinal));
		promptDailyReview.IsPrompt.Should().BeTrue();
	}

	[TestMethod]
	public void SharedServiceProvider_ResolvesModulesAndTransportNeutralServices()
	{
		var app = ManicTimeReplApp.Create();
		var services = ManicTimeReplApp.GetServiceProvider(app);

		services.GetRequiredService<TimelineModule>().Should().NotBeNull();
		services.GetRequiredService<ActivityModule>().Should().NotBeNull();
		services.GetRequiredService<UsageModule>().Should().NotBeNull();
		services.GetRequiredService<SummaryModule>().Should().NotBeNull();
		services.GetRequiredService<ScreenshotModule>().Should().NotBeNull();
		services.GetRequiredService<ResourceModule>().Should().NotBeNull();
		services.GetRequiredService<PromptModule>().Should().NotBeNull();

		services.GetRequiredService<TimelineTools>().Should().NotBeNull();
		services.GetRequiredService<ActivityTools>().Should().NotBeNull();
		services.GetRequiredService<NarrativeTools>().Should().NotBeNull();
		services.GetRequiredService<ManicTimeResources>().Should().NotBeNull();
	}

	[TestMethod]
	public async Task PromptCommands_CanBeExecutedThroughReplTesting()
	{
		await using var host = ReplTestHost.Create(() => ManicTimeReplApp.Create());
		await using var session = await host.OpenSessionAsync().ConfigureAwait(false);

		var execution = await session.RunCommandAsync(
			"prompt daily-review --date 2025-01-15 --no-logo").ConfigureAwait(false);

		execution.ExitCode.Should().Be(0, because: execution.OutputText);
		execution.OutputText.Should().Contain("summary narrative");
		execution.OutputText.Should().Contain("2025-01-15");
		execution.OutputText.Should().Contain("2025-01-16");
	}

	[TestMethod]
	public async Task ContextHelp_CanBeRenderedThroughReplTesting()
	{
		await using var host = ReplTestHost.Create(() => ManicTimeReplApp.Create());
		await using var session = await host.OpenSessionAsync().ConfigureAwait(false);

		var execution = await session.RunCommandAsync(
			"summary --help --no-logo").ConfigureAwait(false);

		execution.ExitCode.Should().Be(0, because: execution.OutputText);
		execution.OutputText.Should().Contain("daily");
		execution.OutputText.Should().Contain("narrative");
		execution.OutputText.Should().Contain("period");
	}
}
