using System.ComponentModel;
using System.Globalization;
using System.Text.Json;
using ManicTimeMcp.Screenshots;
using ModelContextProtocol.Server;

namespace ManicTimeMcp.Mcp;

/// <summary>MCP tools for querying ManicTime screenshots.</summary>
[McpServerToolType]
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
public sealed class ScreenshotTools
{
	private readonly IScreenshotService _screenshotService;

	/// <summary>Creates screenshot tools with injected service.</summary>
	public ScreenshotTools(IScreenshotService screenshotService)
	{
		_screenshotService = screenshotService;
	}

	/// <summary>Returns screenshots within a time window, with optional sampling.</summary>
	[McpServerTool(Name = "get_screenshots", ReadOnly = true), Description("Get screenshots for a date range. Returns metadata and base64-encoded image data. Prefer thumbnails by default.")]
	public string GetScreenshots(
		[Description("Start date (ISO-8601, e.g. 2025-01-15)")] string startDate,
		[Description("End date (ISO-8601, e.g. 2025-01-16)")] string endDate,
		[Description("Minimum minutes between screenshots for sampling (optional)")] int? samplingMinutes = null,
		[Description("Maximum number of screenshots (default 20, max 50)")] int? maxCount = null,
		[Description("Prefer thumbnails over full-size images (default true)")] bool preferThumbnails = true)
	{
		var start = DateTime.ParseExact(startDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);
		var end = DateTime.ParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

		var query = new ScreenshotQuery
		{
			StartLocalTime = start,
			EndLocalTime = end,
			SamplingInterval = samplingMinutes.HasValue ? TimeSpan.FromMinutes(samplingMinutes.Value) : null,
			MaxCount = maxCount,
			PreferThumbnails = preferThumbnails,
		};

		var selection = _screenshotService.Select(query);

		var screenshotResults = new List<object>();
		foreach (var info in selection.Screenshots)
		{
			var bytes = _screenshotService.ReadScreenshot(info.FilePath);
			screenshotResults.Add(new
			{
				timestamp = info.LocalTimestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
				isThumbnail = info.IsThumbnail,
				width = info.Width,
				height = info.Height,
				monitor = info.Monitor,
				hasData = bytes is not null,
				dataBase64 = bytes is not null ? Convert.ToBase64String(bytes) : null,
			});
		}

		return JsonSerializer.Serialize(new
		{
			startDate,
			endDate,
			totalMatching = selection.TotalMatching,
			returned = screenshotResults.Count,
			isTruncated = selection.IsTruncated,
			screenshots = screenshotResults,
		}, JsonOptions.Default);
	}
}
#pragma warning restore IL2026
