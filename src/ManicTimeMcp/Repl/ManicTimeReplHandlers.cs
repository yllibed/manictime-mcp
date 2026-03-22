using System.ComponentModel;
using System.Globalization;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Screenshots;
using Repl;
using Repl.Mcp;

namespace ManicTimeMcp.Repl;

/// <summary>Implements the Repl command handlers for the ManicTime surface.</summary>
internal static class ManicTimeReplHandlers
{
	public static async Task<object> ListTimelinesAsync(
		[FromServices] TimelineTools tools,
		CancellationToken cancellationToken) =>
		ReplToolResultAdapter.FromCallToolResult(
			await tools.GetTimelinesAsync(cancellationToken).ConfigureAwait(false));

	public static async Task<object> ListActivitiesAsync(
		[Description("Timeline identifier returned by timeline list.")] long timelineId,
		[Description("Inclusive activity date range.")] ReplDateRange period,
		ActivityListOptions? options,
		[FromServices] ActivityTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new ActivityListOptions();
		return ReplToolResultAdapter.FromCallToolResult(
			await tools.GetActivitiesAsync(
				timelineId,
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.Limit,
				options.IncludeGroupDetails,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> ListComputerUsageAsync(
		[Description("Inclusive computer-usage date range.")] ReplDateRange period,
		LimitOptions? options,
		[FromServices] ActivityTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new LimitOptions();
		return ReplToolResultAdapter.FromCallToolResult(
			await tools.GetComputerUsageAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.Limit,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> ListTagsAsync(
		[Description("Inclusive tag date range.")] ReplDateRange period,
		LimitOptions? options,
		[FromServices] ActivityTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new LimitOptions();
		return ReplToolResultAdapter.FromCallToolResult(
			await tools.GetTagsAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.Limit,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> ListApplicationUsageAsync(
		[Description("Inclusive application-usage date range.")] ReplDateRange period,
		LimitOptions? options,
		[FromServices] ActivityTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new LimitOptions();
		return ReplToolResultAdapter.FromCallToolResult(
			await tools.GetApplicationUsageAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.Limit,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> ListDocumentUsageAsync(
		[Description("Inclusive document-usage date range.")] ReplDateRange period,
		LimitOptions? options,
		[FromServices] ActivityTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new LimitOptions();
		return ReplToolResultAdapter.FromCallToolResult(
			await tools.GetDocumentUsageAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.Limit,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> ListWebsiteUsageAsync(
		[Description("Inclusive website-usage date range.")] ReplDateRange period,
		WebsiteUsageOptions? options,
		[FromServices] NarrativeTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new WebsiteUsageOptions();
		return ReplToolResultAdapter.FromCallToolResult(
			await tools.GetWebsiteUsageAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.Limit,
				options.MinMinutes,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> BuildDailySummaryAsync(
		[Description("Date to summarize.")] DateOnly date,
		DailySummaryOptions? options,
		[FromServices] NarrativeTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new DailySummaryOptions();
		return ReplToolResultAdapter.FromCallToolResult(
			await tools.GetDailySummaryAsync(
				ToDateLiteral(date),
				options.IncludeSegments,
				options.MinDurationMinutes,
				options.IncludeHourlyWebBreakdown,
				options.MaxGapMinutes,
				options.MaxSegments,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> BuildNarrativeSummaryAsync(
		[Description("Inclusive narrative date range.")] ReplDateRange period,
		NarrativeSummaryOptions? options,
		[FromServices] NarrativeTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new NarrativeSummaryOptions();
		return ReplToolResultAdapter.FromCallToolResult(
			await tools.GetActivityNarrativeAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.IncludeWebsites,
				options.MinDurationMinutes,
				options.MaxGapMinutes,
				options.IncludeSummary,
				options.MaxSegments,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> BuildPeriodSummaryAsync(
		[Description("Inclusive period date range.")] ReplDateRange period,
		[FromServices] NarrativeTools tools,
		CancellationToken cancellationToken) =>
		ReplToolResultAdapter.FromCallToolResult(
			await tools.GetPeriodSummaryAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				cancellationToken).ConfigureAwait(false));

	public static async Task<object> ListScreenshotsAsync(
		[Description("Inclusive screenshot time window.")] ReplDateTimeRange window,
		ScreenshotListOptions? options,
		[FromServices] IScreenshotService screenshotService,
		CancellationToken cancellationToken)
	{
		options ??= new ScreenshotListOptions();
		var selection = await screenshotService.ListScreenshotsAsync(
			new ScreenshotQuery
			{
				StartLocalTime = window.From,
				EndLocalTime = window.To,
				MaxCount = options.MaxCount,
				PreferThumbnails = true,
				SamplingStrategy = ParseSamplingStrategy(options.SamplingStrategy),
			},
			cancellationToken).ConfigureAwait(false);

		return new
		{
			screenshots = selection.Screenshots.Select(static screenshot => new
			{
				screenshotRef = screenshot.Ref,
				timestamp = screenshot.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
				screenshot.Width,
				screenshot.Height,
				screenshot.Monitor,
				screenshot.IsThumbnail,
				resourceUri = $"manictime://screenshot/{screenshot.Ref}",
			}),
			sampling = selection.SamplingStrategyUsed.ToString().ToLowerInvariant(),
			truncation = new
			{
				truncated = selection.IsTruncated,
				returnedCount = selection.Screenshots.Count,
				totalAvailable = selection.TotalMatching,
			},
			diagnostics = DiagnosticsInfo.Ok,
		};
	}

	public static object GetScreenshot(
		[Description("Screenshot reference returned by screenshot list.")] string screenshotRef,
		[FromServices] IScreenshotRegistry registry,
		[FromServices] IScreenshotService screenshotService)
	{
		var info = registry.TryResolve(screenshotRef);
		if (info is null)
		{
			return Results.NotFound("Unknown screenshotRef. Use screenshot list to discover valid references.");
		}

		var thumbnailPath = info.IsThumbnail ? info.FilePath : GetThumbnailPath(info.FilePath);
		var fullSizePath = info.IsThumbnail ? GetFullSizePath(info.FilePath) : info.FilePath;
		var thumbnailBytes = thumbnailPath is not null ? screenshotService.ReadScreenshot(thumbnailPath) : null;
		var fullBytes = fullSizePath is not null ? screenshotService.ReadScreenshot(fullSizePath) : null;
		var selectedBytes = fullBytes ?? thumbnailBytes;
		if (selectedBytes is null)
		{
			return Results.NotFound("Screenshot file not found or inaccessible.");
		}

		return new
		{
			screenshotRef = info.Ref,
			timestamp = info.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
			info.Width,
			info.Height,
			info.Monitor,
			info.IsThumbnail,
			thumbnailBase64 = thumbnailBytes is null ? null : Convert.ToBase64String(thumbnailBytes),
			imageBase64 = Convert.ToBase64String(selectedBytes),
			imageFormat = "image/jpeg",
		};
	}

	public static object CropScreenshot(
		[Description("Screenshot reference returned by screenshot list.")] string screenshotRef,
		[Description("Left crop edge.")] double x,
		[Description("Top crop edge.")] double y,
		[Description("Crop width.")] double width,
		[Description("Crop height.")] double height,
		[Description("Coordinate units: percent or normalized.")] string? coordinateUnits,
		[FromServices] IScreenshotRegistry registry,
		[FromServices] IScreenshotService screenshotService,
		[FromServices] ICropService cropService)
	{
		var info = registry.TryResolve(screenshotRef);
		if (info is null)
		{
			return Results.NotFound("Unknown screenshotRef. Use screenshot list to discover valid references.");
		}

		var fullSizePath = info.IsThumbnail ? GetFullSizePath(info.FilePath) : info.FilePath;
		var sourceBytes = fullSizePath is not null ? screenshotService.ReadScreenshot(fullSizePath) : null;
		sourceBytes ??= screenshotService.ReadScreenshot(info.FilePath);
		if (sourceBytes is null)
		{
			return Results.NotFound("Screenshot file not found or inaccessible.");
		}

		var croppedBytes = cropService.Crop(sourceBytes, new CropRegion
		{
			X = x,
			Y = y,
			Width = width,
			Height = height,
			Units = ParseCoordinateUnits(coordinateUnits),
		});
		if (croppedBytes is null)
		{
			return Results.Validation("Crop failed. The crop region may be invalid or the source image unreadable.");
		}

		return new
		{
			screenshotRef = info.Ref,
			crop = new
			{
				x,
				y,
				width,
				height,
				units = ParseCoordinateUnits(coordinateUnits).ToString().ToLowerInvariant(),
			},
			imageBase64 = Convert.ToBase64String(croppedBytes),
			imageFormat = "image/jpeg",
		};
	}

	public static async Task<object> SaveScreenshotAsync(
		[Description("Screenshot reference returned by screenshot list.")] string screenshotRef,
		ScreenshotSaveOptions? saveOptions,
		ScreenshotCropOptions? cropOptions,
		[FromServices] IScreenshotRegistry registry,
		[FromServices] IScreenshotService screenshotService,
		[FromServices] ICropService cropService,
		IServiceProvider services,
		CancellationToken cancellationToken)
	{
		saveOptions ??= new ScreenshotSaveOptions();
		cropOptions ??= new ScreenshotCropOptions();
		var info = registry.TryResolve(screenshotRef);
		if (info is null)
		{
			return Results.NotFound("Unknown screenshotRef. Use screenshot list to discover valid references.");
		}

		var bytes = ReadFullSizeScreenshot(info, screenshotService);
		if (bytes is null)
		{
			return Results.NotFound("Screenshot file not found or inaccessible.");
		}

		if (HasCrop(cropOptions))
		{
			var cropped = cropService.Crop(bytes, new CropRegion
			{
				X = cropOptions.CropX!.Value,
				Y = cropOptions.CropY!.Value,
				Width = cropOptions.CropWidth!.Value,
				Height = cropOptions.CropHeight!.Value,
				Units = ParseCoordinateUnits(cropOptions.CoordinateUnits),
			});
			if (cropped is null)
			{
				return Results.Validation("Crop failed. The crop region may be invalid or the source image unreadable.");
			}

			bytes = cropped;
		}

		var roots = await ResolveRootsAsync(services, cancellationToken).ConfigureAwait(false);
		return TryWriteScreenshotToRoots(screenshotService, bytes, roots, BuildOutputFileName(saveOptions.OutputPath, info));
	}

	public static string GetConfigResource([FromServices] ManicTimeResources resources) => resources.GetConfig();

	public static Task<string> GetTimelinesResourceAsync(
		[FromServices] ManicTimeResources resources,
		CancellationToken cancellationToken) =>
		resources.GetTimelinesAsync(cancellationToken);

	public static string GetHealthResource([FromServices] ManicTimeResources resources) => resources.GetHealth();

	public static string GetGuideResource() => GuideContent.Text;

	public static Task<string> GetEnvironmentResourceAsync(
		[FromServices] ManicTimeResources resources,
		CancellationToken cancellationToken) =>
		resources.GetEnvironmentAsync(cancellationToken);

	public static Task<string> GetDataRangeResourceAsync(
		[FromServices] ManicTimeResources resources,
		CancellationToken cancellationToken) =>
		resources.GetDataRangeAsync(cancellationToken);

	public static string BuildDailyReviewPrompt([Description("Date to review.")] DateOnly date)
	{
		var nextDay = date.AddDays(1);
		return $"""
			Use `summary narrative --period {ToDateLiteral(date)}..{ToDateLiteral(nextDay)} --includeSummary`.
			Then summarize the day in user-facing language with total active time, top applications, notable context switches, and any screenshot references worth investigating further with `screenshot get` or `screenshot crop`.
			""";
	}

	public static string BuildWeeklyReviewPrompt(
		[Description("Inclusive weekly review date range.")] ReplDateRange period) =>
		$"""
			Use `summary period --period {ToDateLiteral(period.From)}..{ToDateLiteral(period.To)}`.
			Highlight busiest days, quietest days, repeated patterns, and the most important applications and websites across the period.
			""";

	public static string BuildScreenshotInvestigationPrompt(
		[Description("Date-time window for the investigation.")] ReplDateTimeRange window) =>
		$"""
			Use `screenshot list --window {window.From:yyyy-MM-ddTHH:mm:ss}..{window.To:yyyy-MM-ddTHH:mm:ss}` to discover candidates.
			Fetch the best candidate with `screenshot get`, crop the relevant region with `screenshot crop`, and then correlate it with `summary narrative` for the surrounding day.
			""";

	private static byte[]? ReadFullSizeScreenshot(ScreenshotInfo info, IScreenshotService screenshotService)
	{
		var fullSizePath = info.IsThumbnail ? GetFullSizePath(info.FilePath) : info.FilePath;
		var bytes = fullSizePath is not null ? screenshotService.ReadScreenshot(fullSizePath) : null;
		return bytes ?? screenshotService.ReadScreenshot(info.FilePath);
	}

	private static async Task<IReadOnlyList<McpClientRoot>> ResolveRootsAsync(
		IServiceProvider services,
		CancellationToken cancellationToken)
	{
		var clientRoots = services.GetService(typeof(IMcpClientRoots)) as IMcpClientRoots;
		if (clientRoots is null)
		{
			return [];
		}

		var roots = await clientRoots.GetAsync(cancellationToken).ConfigureAwait(false);
		if (roots.Count > 0)
		{
			return roots;
		}

		if (clientRoots.IsSupported || clientRoots.HasSoftRoots)
		{
			return clientRoots.Current;
		}

		return [];
	}

	private static object TryWriteScreenshotToRoots(
		IScreenshotService screenshotService,
		byte[] bytes,
		IReadOnlyList<McpClientRoot> roots,
		string relativePath)
	{
		if (roots.Count == 0)
		{
			return Results.Error("mcp_roots_required", "No MCP roots are available for this session.");
		}

		foreach (var root in roots)
		{
			if (!root.Uri.IsFile)
			{
				continue;
			}

			var rootDirectory = Path.GetFullPath(root.Uri.LocalPath);
			var absolutePath = Path.GetFullPath(Path.Combine(rootDirectory, relativePath));
			var normalizedRoot = rootDirectory.EndsWith(Path.DirectorySeparatorChar)
				? rootDirectory
				: rootDirectory + Path.DirectorySeparatorChar;

			if (!absolutePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			var written = screenshotService.WriteScreenshot(bytes, absolutePath, rootDirectory);
			if (written < 0)
			{
				return Results.Error("screenshot_write_failed", "Failed to save the screenshot inside the requested root.");
			}

			return new
			{
				path = absolutePath,
				size = written,
			};
		}

		return Results.Validation($"Output path '{relativePath}' does not resolve inside any declared MCP root.");
	}

	private static bool HasCrop(ScreenshotCropOptions options) =>
		options.CropX.HasValue
		&& options.CropY.HasValue
		&& options.CropWidth.HasValue
		&& options.CropHeight.HasValue;

	private static string ToDateLiteral(DateOnly date) =>
		date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

	private static string GetThumbnailPath(string filePath) =>
		filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
			? string.Concat(filePath.AsSpan(0, filePath.Length - 4), ".thumbnail.jpg")
			: filePath;

	private static string? GetFullSizePath(string filePath) =>
		filePath.Contains(".thumbnail.", StringComparison.OrdinalIgnoreCase)
			? filePath.Replace(".thumbnail.", ".", StringComparison.OrdinalIgnoreCase)
			: filePath;

	private static SamplingStrategy ParseSamplingStrategy(string? value) =>
		value?.Trim().ToUpperInvariant() switch
		{
			"INTERVAL" => SamplingStrategy.Interval,
			_ => SamplingStrategy.ActivityTransition,
		};

	private static CoordinateUnits ParseCoordinateUnits(string? value) =>
		value?.Trim().ToUpperInvariant() switch
		{
			"NORMALIZED" => CoordinateUnits.Normalized,
			_ => CoordinateUnits.Percent,
		};

	private static string BuildOutputFileName(string? outputPath, ScreenshotInfo info)
	{
		var fileName = string.IsNullOrWhiteSpace(outputPath)
			? $"screenshot-{info.LocalTimestamp.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture)}"
			: outputPath;

		if (!fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
			&& !fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
			&& !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
		{
			fileName += ".jpg";
		}

		return fileName;
	}
}
