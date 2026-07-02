using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using ManicTimeMcp.Database.Dto;
using ManicTimeMcp.Mcp;
using ManicTimeMcp.Models;
using ManicTimeMcp.Screenshots;
using Repl;
using Repl.Mcp;

namespace ManicTimeMcp.Repl;

/// <summary>Implements the Repl command handlers for the ManicTime surface.</summary>
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
internal static class ManicTimeReplHandlers
{
	public static async Task<object> ListTimelinesAsync(
		[FromServices] TimelineTools tools,
		CancellationToken cancellationToken) =>
		ReplToolResultAdapter.FromToolResult(
			await tools.GetTimelinesAsync(cancellationToken).ConfigureAwait(false));

	public static async Task<object> ListActivitiesAsync(
		[Description("Timeline ID from timeline list (e.g. 1 for Applications, 4 for Documents).")] long timelineId,
		[Description("Date range as YYYY-MM-DD..YYYY-MM-DD (e.g. 2026-04-01..2026-04-07).")] ReplDateRange period,
		ActivityListOptions? options,
		[FromServices] ActivityTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new ActivityListOptions();
		return ReplToolResultAdapter.FromToolResult(
			await tools.GetActivitiesAsync(
				timelineId,
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.Limit,
				options.IncludeGroupDetails,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> ListComputerUsageAsync(
		[Description("Date range as YYYY-MM-DD..YYYY-MM-DD (e.g. 2026-04-01..2026-04-07).")] ReplDateRange period,
		LimitOptions? options,
		[FromServices] ActivityTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new LimitOptions();
		return ReplToolResultAdapter.FromToolResult(
			await tools.GetComputerUsageAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.Limit,
				cancellationToken).ConfigureAwait(false));
	}


	public static async Task<object> ListUsageSummaryAsync(
		[Description("Date range as YYYY-MM-DD..YYYY-MM-DD (e.g. 2026-04-01..2026-04-07).")] ReplDateRange period,
		UsageSummaryOptions? options,
		[FromServices] ActivityTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new UsageSummaryOptions();
		return ReplToolResultAdapter.FromToolResult(
			await tools.GetUsageSummaryAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				options.Type,
				options.Limit,
				options.MinMinutes,
				cancellationToken).ConfigureAwait(false));
	}

	public static async Task<object> BuildDailySummaryAsync(
		[Description("Date as YYYY-MM-DD (e.g. 2026-04-12).")] DateOnly date,
		DailySummaryOptions? options,
		[FromServices] NarrativeTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new DailySummaryOptions();
		return ReplToolResultAdapter.FromToolResult(
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
		[Description("Date range as YYYY-MM-DD..YYYY-MM-DD (e.g. 2026-04-01..2026-04-07). Single day: use same date for both.")] ReplDateRange period,
		NarrativeSummaryOptions? options,
		[FromServices] NarrativeTools tools,
		CancellationToken cancellationToken)
	{
		options ??= new NarrativeSummaryOptions();
		return ReplToolResultAdapter.FromToolResult(
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
		[Description("Date range as YYYY-MM-DD..YYYY-MM-DD (e.g. 2026-04-07..2026-04-13 for a week).")] ReplDateRange period,
		[FromServices] NarrativeTools tools,
		CancellationToken cancellationToken) =>
		ReplToolResultAdapter.FromToolResult(
			await tools.GetPeriodSummaryAsync(
				ToDateLiteral(period.From),
				ToDateLiteral(period.To),
				cancellationToken).ConfigureAwait(false));

	public static async Task<object> ListScreenshotsAsync(
		[Description("DateTime range as YYYY-MM-DDThh:mm:ss..YYYY-MM-DDThh:mm:ss (e.g. 2026-04-12T09:00:00..2026-04-12T10:00:00).")] ReplDateTimeRange window,
		ScreenshotListOptions? options,
		[FromServices] IScreenshotService screenshotService,
		[FromServices] IScreenshotRegistry registry,
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

		var screenshots = selection.Screenshots
			.Select(screenshot => new
			{
				Info = screenshot,
				Ref = screenshot.Ref ?? registry.Register(screenshot),
			})
			.ToArray();

		return new
		{
			screenshots = screenshots.Select(static screenshot => new
			{
				screenshotRef = screenshot.Ref,
				timestamp = screenshot.Info.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
				displayLocalTime = screenshot.Info.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
				screenshot.Info.Width,
				screenshot.Info.Height,
				screenshot.Info.Monitor,
				hasThumbnail = HasThumbnailVariant(screenshot.Info),
			}).ToArray(),
			sampling = selection.SamplingStrategyUsed.ToString().ToLowerInvariant(),
			truncation = new
			{
				truncated = selection.IsTruncated,
				returnedCount = screenshots.Length,
				totalAvailable = selection.TotalMatching,
			},
			diagnostics = DiagnosticsInfo.Ok,
		};
	}

	public static object GetScreenshot(
		[Description("Opaque reference from screenshot list. Always discover via screenshot list first.")] string screenshotRef,
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
		byte[]? fullBytes = null;
		var selectedBytes = thumbnailBytes;
		if (selectedBytes is null)
		{
			fullBytes = fullSizePath is not null ? screenshotService.ReadScreenshot(fullSizePath) : null;
			selectedBytes = fullBytes;
		}

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

	public static object GetScreenshotResource(
		[Description("Opaque reference from screenshot list. Always discover via screenshot list first.")] string screenshotRef,
		[FromServices] IScreenshotRegistry registry,
		[FromServices] IScreenshotService screenshotService)
	{
		var result = GetScreenshot(screenshotRef, registry, screenshotService);

		// Propagate Repl error results (NotFound, Validation) directly
		// instead of serializing them as successful JSON strings.
		if (result is IReplResult)
		{
			return result;
		}

		return JsonSerializer.Serialize(result, JsonOptions.Indented);
	}

	public static object CropScreenshot(
		[Description("Opaque reference from screenshot list. Always discover via screenshot list first.")] string screenshotRef,
		[Description("Left edge of crop rectangle (0-100 in percent mode).")] double x,
		[Description("Top edge of crop rectangle (0-100 in percent mode).")] double y,
		[Description("Width of crop rectangle (0-100 in percent mode).")] double width,
		[Description("Height of crop rectangle (0-100 in percent mode).")] double height,
		[Description("Coordinate system: 'percent' (0-100, default) or 'normalized' (0-1).")] string? coordinateUnits,
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
		[Description("Opaque reference from screenshot list. Always discover via screenshot list first.")] string screenshotRef,
		ScreenshotSaveOptions? saveOptions,
		ScreenshotCropOptions? cropOptions,
		[FromServices] IScreenshotRegistry registry,
		[FromServices] IScreenshotService screenshotService,
		[FromServices] ICropService cropService,
		[FromServices] IMcpClientRoots clientRoots,
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

		if (TryCreateCropRegion(cropOptions, out var cropRegion, out var validationFailure))
		{
			var cropped = cropService.Crop(bytes, cropRegion!);
			if (cropped is null)
			{
				return Results.Validation("Crop failed. The crop region may be invalid or the source image unreadable.");
			}

			bytes = cropped;
		}
		else if (validationFailure is not null)
		{
			return Results.Validation(validationFailure);
		}

		var roots = await ResolveRootsAsync(clientRoots, cancellationToken).ConfigureAwait(false);
		return TryWriteScreenshotToRoots(screenshotService, bytes, roots, BuildOutputFileName(saveOptions.OutputPath, info));
	}

	public static object InitializeWorkspaceRoots(
		[Description("Absolute directory path that should become the session workspace root.")] string path,
		[FromServices] IMcpClientRoots clientRoots)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return Results.Validation("Workspace path is required.");
		}

		if (!Path.IsPathRooted(path))
		{
			return Results.Validation("Workspace path must be absolute.");
		}

		var fullPath = Path.GetFullPath(path);
		if (!Directory.Exists(fullPath))
		{
			return Results.Validation($"Workspace path '{fullPath}' does not exist.");
		}

		var normalizedPath = Path.EndsInDirectorySeparator(fullPath)
			? fullPath
			: fullPath + Path.DirectorySeparatorChar;
		var displayName = Path.GetFileName(fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		if (string.IsNullOrWhiteSpace(displayName))
		{
			displayName = fullPath;
		}

		clientRoots.SetSoftRoots(
		[
			new McpClientRoot(new Uri(normalizedPath, UriKind.Absolute), displayName),
		]);

		return new
		{
			path = fullPath,
			name = displayName,
			mode = clientRoots.IsSupported ? "native-plus-soft" : "soft-roots",
		};
	}

	public static ConfigResource GetConfigResource([FromServices] ManicTimeResources resources) => resources.GetConfig();

	public static Task<IReadOnlyList<TimelineDto>> GetTimelinesResourceAsync(
		[FromServices] ManicTimeResources resources,
		CancellationToken cancellationToken) =>
		resources.GetTimelinesAsync(cancellationToken);

	public static HealthReport GetHealthResource([FromServices] ManicTimeResources resources) => resources.GetHealth();

	public static string GetGuideResource() => GuideContent.Text;

	public static Task<EnvironmentResource> GetEnvironmentResourceAsync(
		[FromServices] ManicTimeResources resources,
		CancellationToken cancellationToken) =>
		resources.GetEnvironmentAsync(cancellationToken);

	public static Task<DataRangeResource> GetDataRangeResourceAsync(
		[FromServices] ManicTimeResources resources,
		CancellationToken cancellationToken) =>
		resources.GetDataRangeAsync(cancellationToken);

	public static string BuildDailyReviewPrompt([Description("Date as YYYY-MM-DD (e.g. 2026-04-12).")] DateOnly date)
	{
		var nextDay = date.AddDays(1);
		return $"""
			Use `summary narrative --period {ToDateLiteral(date)}..{ToDateLiteral(nextDay)} --includeSummary`.
			Then summarize the day in user-facing language with total active time, top applications, notable context switches, and any screenshot references worth investigating further with `screenshot get` or `screenshot crop`.
			""";
	}

	public static string BuildWeeklyReviewPrompt(
		[Description("Date range as YYYY-MM-DD..YYYY-MM-DD (e.g. 2026-04-07..2026-04-13).")] ReplDateRange period) =>
		$"""
			Use `summary period --period {ToDateLiteral(period.From)}..{ToDateLiteral(period.To)}`.
			Highlight busiest days, quietest days, repeated patterns, and the most important applications and websites across the period.
			""";

	public static string BuildScreenshotInvestigationPrompt(
		[Description("DateTime range as YYYY-MM-DDThh:mm:ss..YYYY-MM-DDThh:mm:ss (e.g. 2026-04-12T09:00:00..2026-04-12T10:00:00).")] ReplDateTimeRange window) =>
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
		IMcpClientRoots clientRoots,
		CancellationToken cancellationToken)
	{
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

	private static bool TryCreateCropRegion(
		ScreenshotCropOptions options,
		out CropRegion? cropRegion,
		out string? validationFailure)
	{
		var cropValues = new[]
		{
			options.CropX.HasValue,
			options.CropY.HasValue,
			options.CropWidth.HasValue,
			options.CropHeight.HasValue,
		};

		if (cropValues.All(static value => !value))
		{
			cropRegion = default;
			validationFailure = null;
			return false;
		}

		if (cropValues.Any(static value => !value))
		{
			cropRegion = default;
			validationFailure = "Provide either all crop values (cropX, cropY, cropWidth, cropHeight) or none of them.";
			return false;
		}

		cropRegion = new CropRegion
		{
			X = options.CropX!.Value,
			Y = options.CropY!.Value,
			Width = options.CropWidth!.Value,
			Height = options.CropHeight!.Value,
			Units = ParseCoordinateUnits(options.CoordinateUnits),
		};
		validationFailure = null;
		return true;
	}

	private static string ToDateLiteral(DateOnly date) =>
		date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

	private static string? GetThumbnailPath(string filePath) =>
		filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
			? string.Concat(filePath.AsSpan(0, filePath.Length - 4), ".thumbnail.jpg")
			: null;

	private static string? GetFullSizePath(string filePath) =>
		filePath.Contains(".thumbnail.", StringComparison.OrdinalIgnoreCase)
			? filePath.Replace(".thumbnail.", ".", StringComparison.OrdinalIgnoreCase)
			: filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
				? filePath
				: null;

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

	private static bool HasThumbnailVariant(ScreenshotInfo screenshot)
	{
		if (screenshot.IsThumbnail)
		{
			return true;
		}

		var thumbnailPath = GetThumbnailPath(screenshot.FilePath);
		return thumbnailPath is not null && File.Exists(thumbnailPath);
	}
}
#pragma warning restore IL2026
