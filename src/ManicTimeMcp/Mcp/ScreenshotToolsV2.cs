using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ManicTimeMcp.Screenshots;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;
using McpServer = ModelContextProtocol.Server.McpServer;

namespace ManicTimeMcp.Mcp;

/// <summary>Progressive-resolution MCP tools for screenshots.</summary>
[McpServerToolType]
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
public sealed class ScreenshotToolsV2
{
	private readonly IScreenshotService _screenshotService;
	private readonly IScreenshotRegistry _registry;
	private readonly ICropService _cropService;

	/// <summary>Creates screenshot tools with injected services.</summary>
	public ScreenshotToolsV2(
		IScreenshotService screenshotService,
		IScreenshotRegistry registry,
		ICropService cropService)
	{
		_screenshotService = screenshotService;
		_registry = registry;
		_cropService = cropService;
	}

	/// <summary>Lists screenshots with metadata only (zero image bytes).</summary>
	[McpServerTool(Name = "list_screenshots", ReadOnly = true), Description("List screenshots for a date range. Returns metadata and resource links (no image bytes). Use get_screenshot to retrieve images.")]
	public async Task<CallToolResult> ListScreenshotsAsync(
		[Description("Start date (ISO-8601, inclusive)")] string startDate,
		[Description("End date (ISO-8601, exclusive)")] string endDate,
		[Description("Maximum screenshots (default 20, max 100)")] int? maxCount = null,
		[Description("Sampling strategy: 'activity_transition' (default) or 'interval'")] string? samplingStrategy = null,
		CancellationToken cancellationToken = default)
	{
		try
		{
			var (start, end) = ParseDates(startDate, endDate);
			var strategy = ParseSamplingStrategy(samplingStrategy);

			var query = new ScreenshotQuery
			{
				StartLocalTime = start,
				EndLocalTime = end,
				MaxCount = maxCount,
				PreferThumbnails = true,
				SamplingStrategy = strategy,
			};

			var selection = await _screenshotService.ListScreenshotsAsync(query, cancellationToken)
				.ConfigureAwait(false);

			return BuildListResult(selection);
		}
		catch (FormatException ex)
		{
			return ToolResults.Error($"Invalid date format. Expected ISO-8601 (yyyy-MM-dd). {ex.Message}");
		}
	}

	/// <summary>Retrieves a single screenshot with dual-audience delivery.</summary>
	[McpServerTool(Name = "get_screenshot", ReadOnly = true), Description("Get a screenshot by reference. Returns a thumbnail for model reasoning and full image for human viewing.")]
	public CallToolResult GetScreenshot(
		[Description("Screenshot reference from list_screenshots")] string screenshotRef)
	{
		var info = _registry.TryResolve(screenshotRef);
		if (info is null)
		{
			return ToolResults.Error("Unknown screenshotRef. Use list_screenshots to discover valid references.");
		}

		try
		{
			return BuildGetResult(info);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return ToolResults.Error($"Screenshot file error: {ex.Message}. Try reading the manictime://health resource to diagnose the issue.");
		}
	}

	/// <summary>Crops a region from a screenshot.</summary>
	[McpServerTool(Name = "crop_screenshot", ReadOnly = true), Description("Crop a region from a screenshot using percentage or normalized coordinates. Designed for model-driven ROI selection after inspecting a thumbnail.")]
	public CallToolResult CropScreenshot(
		[Description("Screenshot reference from list_screenshots")] string screenshotRef,
		[Description("Left edge (percent 0-100 or normalized 0.0-1.0)")] double x,
		[Description("Top edge (percent 0-100 or normalized 0.0-1.0)")] double y,
		[Description("Width (percent 0-100 or normalized 0.0-1.0)")] double width,
		[Description("Height (percent 0-100 or normalized 0.0-1.0)")] double height,
		[Description("Coordinate units: 'percent' (default) or 'normalized'")] string? coordinateUnits = null)
	{
		var info = _registry.TryResolve(screenshotRef);
		if (info is null)
		{
			return ToolResults.Error("Unknown screenshotRef. Use list_screenshots to discover valid references.");
		}

		try
		{
			return BuildCropResult(info, x, y, width, height, coordinateUnits);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			return ToolResults.Error($"Screenshot file error: {ex.Message}. Try reading the manictime://health resource to diagnose the issue.");
		}
	}

	/// <summary>Saves a screenshot to the filesystem within a client-declared MCP root.</summary>
	[McpServerTool(Name = "save_screenshot"), Description("Save a screenshot to disk. The output path must be within a client-declared MCP root directory. Returns the absolute path and file size.")]
	public async Task<CallToolResult> SaveScreenshotAsync(
		McpServer server,
		[Description("Screenshot reference from list_screenshots")] string screenshotRef,
		[Description("Relative path + filename (e.g. 'assets/screenshot-0930'). Extension (.jpg) is appended automatically if missing.")] string? outputPath = null,
		[Description("Left edge for optional crop (percent 0-100 or normalized 0.0-1.0)")] double? cropX = null,
		[Description("Top edge for optional crop")] double? cropY = null,
		[Description("Width for optional crop")] double? cropWidth = null,
		[Description("Height for optional crop")] double? cropHeight = null,
		[Description("Coordinate units for crop: 'percent' (default) or 'normalized'")] string? coordinateUnits = null,
		CancellationToken cancellationToken = default)
	{
		var info = _registry.TryResolve(screenshotRef);
		if (info is null)
		{
			return ToolResults.Error("Unknown screenshotRef. Use list_screenshots to discover valid references.");
		}

		var roots = await ResolveClientRootsAsync(server, cancellationToken).ConfigureAwait(false);
		if (roots is null)
		{
			return ToolResults.Error("Client does not support MCP roots. Configure roots in your MCP client to use save_screenshot.");
		}

		if (roots.Count == 0)
		{
			return ToolResults.Error("No MCP roots declared by client. Configure at least one root directory to use save_screenshot.");
		}

		var bytes = ReadFullSizeScreenshot(info);
		if (bytes is null)
		{
			return ToolResults.Error("Screenshot file not found or inaccessible.");
		}

		bytes = ApplyOptionalCrop(bytes, cropX, cropY, cropWidth, cropHeight, coordinateUnits);
		if (bytes is null)
		{
			return ToolResults.Error("Crop failed. The image may be corrupted or the crop region is invalid.");
		}

		var fileName = BuildOutputFileName(outputPath, info);
		return WriteToFirstMatchingRoot(bytes, fileName, roots);
	}

	private static async Task<IList<Root>?> ResolveClientRootsAsync(
		McpServer server, CancellationToken ct)
	{
		try
		{
			if (server.ClientCapabilities?.Roots is null)
			{
				return null;
			}

			var result = await server.RequestRootsAsync(new ListRootsRequestParams(), ct).ConfigureAwait(false);
			return result.Roots;
		}
		catch (InvalidOperationException)
		{
			return null;
		}
	}

	private byte[]? ReadFullSizeScreenshot(ScreenshotInfo info)
	{
		var fullPath = info.IsThumbnail ? GetFullSizePath(info.FilePath) : info.FilePath;
		var bytes = fullPath is not null ? _screenshotService.ReadScreenshot(fullPath) : null;
		return bytes ?? _screenshotService.ReadScreenshot(info.FilePath);
	}

	private byte[]? ApplyOptionalCrop(
		byte[] bytes, double? cropX, double? cropY, double? cropWidth, double? cropHeight, string? coordinateUnits)
	{
		if (cropX is null || cropY is null || cropWidth is null || cropHeight is null)
		{
			return bytes;
		}

		var region = new CropRegion
		{
			X = cropX.Value, Y = cropY.Value,
			Width = cropWidth.Value, Height = cropHeight.Value,
			Units = ParseCoordinateUnits(coordinateUnits),
		};
		return _cropService.Crop(bytes, region);
	}

	private static string BuildOutputFileName(string? outputPath, ScreenshotInfo info)
	{
		var fileName = outputPath
			?? $"screenshot-{info.LocalTimestamp.ToString("yyyy-MM-dd-HHmmss", CultureInfo.InvariantCulture)}";
		if (!fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
			&& !fileName.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
			&& !fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
		{
			fileName += ".jpg";
		}

		return fileName;
	}

	private CallToolResult WriteToFirstMatchingRoot(byte[] bytes, string fileName, IList<Root> roots)
	{
		foreach (var root in roots)
		{
			var rootDir = RootUriToLocalPath(root.Uri);
			if (rootDir is null)
			{
				continue;
			}

			var absolutePath = Path.GetFullPath(Path.Combine(rootDir, fileName));
			var normalizedRoot = Path.GetFullPath(rootDir);
			if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar))
			{
				normalizedRoot += Path.DirectorySeparatorChar;
			}

			if (!absolutePath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
			{
				continue; // Path escapes this root — try next
			}

			var written = _screenshotService.WriteScreenshot(bytes, absolutePath, rootDir);
			if (written < 0)
			{
				return ToolResults.Error("Failed to save screenshot. The path may be invalid or inaccessible.");
			}

			return ToolResults.Success(JsonSerializer.Serialize(new
			{
				path = absolutePath,
				size = written,
			}, JsonOptions.Default));
		}

		return ToolResults.Error(
			$"Output path '{fileName}' does not resolve within any declared MCP root. Declared roots: {string.Join(", ", roots.Select(r => r.Uri))}");
	}

	/// <summary>Converts a file:/// URI to a local filesystem path.</summary>
	internal static string? RootUriToLocalPath(string fileUri)
	{
		if (Uri.TryCreate(fileUri, UriKind.Absolute, out var uri) && uri.IsFile)
		{
			return uri.LocalPath;
		}

		return null;
	}

	private static CallToolResult BuildListResult(ScreenshotSelection selection)
	{
		var content = new List<ContentBlock>();

		var metadata = new
		{
			screenshots = selection.Screenshots.Select(s => new
			{
				screenshotRef = s.Ref,
				timestamp = s.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
				displayLocalTime = s.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
				s.Width,
				s.Height,
				s.Monitor,
				hasThumbnail = s.IsThumbnail || HasThumbnailVariant(s),
				resourceUri = $"manictime://screenshot/{s.Ref}",
			}),
			sampling = selection.SamplingStrategyUsed.ToString().ToLowerInvariant(),
			truncation = new TruncationInfo
			{
				Truncated = selection.IsTruncated,
				ReturnedCount = selection.Screenshots.Count,
				TotalAvailable = selection.TotalMatching,
			},
			diagnostics = DiagnosticsInfo.Ok,
		};

		content.Add(new TextContentBlock
		{
			Text = JsonSerializer.Serialize(metadata, JsonOptions.Default),
		});

		foreach (var screenshot in selection.Screenshots)
		{
			content.Add(new ResourceLinkBlock
			{
				Uri = $"manictime://screenshot/{screenshot.Ref}",
				Name = $"Screenshot {screenshot.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}",
				MimeType = "image/jpeg",
			});
		}

		return new CallToolResult { Content = content };
	}

	private CallToolResult BuildGetResult(ScreenshotInfo info)
	{
		var content = new List<ContentBlock>();
		var thumbnailPath = info.IsThumbnail ? info.FilePath : GetThumbnailPath(info.FilePath);
		var fullPath = info.IsThumbnail ? GetFullSizePath(info.FilePath) : info.FilePath;
		var bothAudience = new Annotations { Audience = [Role.User, Role.Assistant] };
		var userOnly = new Annotations { Audience = [Role.User] };

		// Read thumbnail for model reasoning
		var thumbnailBytes = thumbnailPath is not null ? _screenshotService.ReadScreenshot(thumbnailPath) : null;
		var fullBytes = fullPath is not null ? _screenshotService.ReadScreenshot(fullPath) : null;

		if (thumbnailBytes is not null && fullBytes is not null)
		{
			// Dual-audience: thumbnail for model + full for human
			content.Add(new ImageContentBlock
			{
				Data = Convert.ToBase64String(thumbnailBytes),
				MimeType = "image/jpeg",
				Annotations = bothAudience,
			});
			content.Add(new ImageContentBlock
			{
				Data = Convert.ToBase64String(fullBytes),
				MimeType = "image/jpeg",
				Annotations = userOnly,
			});
		}
		else
		{
			// Single image available — both audiences see it
			var bytes = thumbnailBytes ?? fullBytes;
			if (bytes is not null)
			{
				content.Add(new ImageContentBlock
				{
					Data = Convert.ToBase64String(bytes),
					MimeType = "image/jpeg",
					Annotations = bothAudience,
				});
			}
		}

		content.Add(new TextContentBlock
		{
			Text = JsonSerializer.Serialize(new
			{
				screenshotRef = info.Ref,
				timestamp = info.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
				info.Width,
				info.Height,
				info.Monitor,
				isThumbnail = info.IsThumbnail,
			}, JsonOptions.Default),
		});

		return new CallToolResult { Content = content };
	}

	private CallToolResult BuildCropResult(
		ScreenshotInfo info, double x, double y, double w, double h, string? coordinateUnits)
	{
		var units = ParseCoordinateUnits(coordinateUnits);

		// Always read full-size for crop extraction
		var fullPath = info.IsThumbnail ? GetFullSizePath(info.FilePath) : info.FilePath;
		var bytes = fullPath is not null ? _screenshotService.ReadScreenshot(fullPath) : null;
		bytes ??= _screenshotService.ReadScreenshot(info.FilePath);

		if (bytes is null)
		{
			return ToolResults.Error("Screenshot file not found or inaccessible.");
		}

		var region = new CropRegion { X = x, Y = y, Width = w, Height = h, Units = units };
		var cropped = _cropService.Crop(bytes, region);
		if (cropped is null)
		{
			return ToolResults.Error("Crop failed. The image may be corrupted or the region is invalid.");
		}

		var content = new List<ContentBlock>
		{
			new ImageContentBlock
			{
				Data = Convert.ToBase64String(cropped),
				MimeType = "image/jpeg",
				Annotations = new Annotations { Audience = [Role.User, Role.Assistant] },
			},
			new TextContentBlock
			{
				Text = JsonSerializer.Serialize(new
				{
					screenshotRef = info.Ref,
					crop = new { x, y, width = w, height = h, units = units.ToString().ToLowerInvariant() },
				}, JsonOptions.Default),
			},
		};

		return new CallToolResult { Content = content };
	}

	private static (DateTime Start, DateTime End) ParseDates(string startDate, string endDate)
	{
		var start = DateTime.ParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		var end = DateTime.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		return (start, end);
	}

	private static SamplingStrategy ParseSamplingStrategy(string? value) =>
		value?.ToUpperInvariant() switch
		{
			"INTERVAL" => SamplingStrategy.Interval,
			_ => SamplingStrategy.ActivityTransition,
		};

	private static CoordinateUnits ParseCoordinateUnits(string? value) =>
		value?.ToUpperInvariant() switch
		{
			"NORMALIZED" => CoordinateUnits.Normalized,
			_ => CoordinateUnits.Percent,
		};

	private static bool HasThumbnailVariant(ScreenshotInfo info) =>
		!info.IsThumbnail && File.Exists(GetThumbnailPath(info.FilePath));

	private static string? GetThumbnailPath(string filePath) =>
		filePath.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
			? string.Concat(filePath.AsSpan(0, filePath.Length - 4), ".thumbnail.jpg")
			: null;

	private static string? GetFullSizePath(string filePath) =>
		filePath.Contains(".thumbnail.", StringComparison.OrdinalIgnoreCase)
			? filePath.Replace(".thumbnail.", ".", StringComparison.OrdinalIgnoreCase)
			: null;
}
#pragma warning restore IL2026
