using ManicTimeMcp.Configuration;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Provides screenshot selection from the ManicTime data directory
/// with time-window filtering, interval sampling, and secure file access.
/// </summary>
public sealed class ScreenshotService : IScreenshotService
{
	/// <summary>Hard cap on screenshots returned regardless of caller input.</summary>
	internal const int MaxScreenshots = 50;

	/// <summary>Default number of screenshots when no limit is specified.</summary>
	internal const int DefaultMaxScreenshots = 20;

	private const string JpgExtension = ".jpg";
	private const string ScreenshotDirectoryName = "Screenshots";

	private readonly IDataDirectoryResolver _resolver;
	private readonly IScreenshotRegistry _registry;
	private readonly IActivityTransitionProvider _transitionProvider;
	private readonly ILogger<ScreenshotService> _logger;

	/// <summary>Creates a new screenshot service.</summary>
	public ScreenshotService(
		IDataDirectoryResolver resolver,
		IScreenshotRegistry registry,
		IActivityTransitionProvider transitionProvider,
		ILogger<ScreenshotService> logger)
	{
		_resolver = resolver;
		_registry = registry;
		_transitionProvider = transitionProvider;
		_logger = logger;
	}

	/// <inheritdoc />
	public ScreenshotSelection Select(ScreenshotQuery query)
	{
		var screenshotDir = ResolveScreenshotDirectory();
		if (screenshotDir is null || !Directory.Exists(screenshotDir))
		{
			return EmptySelection();
		}

		var allParsed = ScanAndParse(screenshotDir);
		if (allParsed.Count == 0)
		{
			return EmptySelection();
		}

		// Filter by time window
		var matching = allParsed
			.Where(s => s.LocalTimestamp >= query.StartLocalTime && s.LocalTimestamp < query.EndLocalTime)
			.OrderBy(s => s.LocalTimestamp)
			.ToList();

		var totalMatching = matching.Count;

		// Apply thumbnail preference: group by timestamp and pick preferred variant
		if (query.PreferThumbnails)
		{
			matching = PreferVariant(matching, preferThumbnail: true);
		}
		else
		{
			matching = PreferVariant(matching, preferThumbnail: false);
		}

		// Apply interval sampling
		if (query.SamplingInterval is { } interval && interval > TimeSpan.Zero)
		{
			matching = ApplySampling(matching, interval);
		}

		// Apply limit
		var effectiveLimit = Math.Min(query.MaxCount ?? DefaultMaxScreenshots, MaxScreenshots);
		var isTruncated = matching.Count > effectiveLimit;
		if (isTruncated)
		{
			matching = matching.Take(effectiveLimit).ToList();
		}

		_logger.ScreenshotsSelected(matching.Count, totalMatching);

		return new ScreenshotSelection
		{
			Screenshots = matching.AsReadOnly(),
			TotalMatching = totalMatching,
			IsTruncated = isTruncated,
		};
	}

	/// <inheritdoc />
	public async Task<ScreenshotSelection> ListScreenshotsAsync(
		ScreenshotQuery query, CancellationToken cancellationToken = default)
	{
		var (matching, totalMatching) = ScanAndFilter(query);
		if (matching.Count == 0)
		{
			return EmptySelection();
		}

		// Apply sampling strategy
		var (sampled, strategyUsed) = await ApplySamplingStrategyAsync(
			matching, query, cancellationToken).ConfigureAwait(false);

		// Apply limit and register
		return BuildListResult(sampled, totalMatching, strategyUsed, query.MaxCount);
	}

	private (List<ScreenshotInfo> Matching, int TotalMatching) ScanAndFilter(ScreenshotQuery query)
	{
		var screenshotDir = ResolveScreenshotDirectory();
		if (screenshotDir is null || !Directory.Exists(screenshotDir))
		{
			return ([], 0);
		}

		var allParsed = ScanAndParse(screenshotDir);
		if (allParsed.Count == 0)
		{
			return ([], 0);
		}

		var matching = allParsed
			.Where(s => s.LocalTimestamp >= query.StartLocalTime && s.LocalTimestamp < query.EndLocalTime)
			.OrderBy(s => s.LocalTimestamp)
			.ToList();

		var totalMatching = matching.Count;
		matching = PreferVariant(matching, preferThumbnail: query.PreferThumbnails);
		return (matching, totalMatching);
	}

	private async Task<(List<ScreenshotInfo> Result, SamplingStrategy StrategyUsed)> ApplySamplingStrategyAsync(
		List<ScreenshotInfo> matching, ScreenshotQuery query, CancellationToken cancellationToken)
	{
		var strategyUsed = query.SamplingStrategy;

		if (query.SamplingStrategy == SamplingStrategy.ActivityTransition)
		{
			var transitions = await _transitionProvider.GetTransitionsAsync(
				query.StartLocalTime, query.EndLocalTime, cancellationToken).ConfigureAwait(false);

			if (transitions.Count > 0)
			{
				return (ActivityTransitionSampler.Sample(matching, transitions), strategyUsed);
			}

			// Fall back to interval sampling if no transitions found
			strategyUsed = SamplingStrategy.Interval;
		}

		// Use explicit interval if provided; otherwise compute one that distributes
		// screenshots evenly across the time range (avoids returning unsampled results).
		var interval = query.SamplingInterval;
		if ((interval is null || interval <= TimeSpan.Zero) && matching.Count > 0)
		{
			var effectiveMax = Math.Min(query.MaxCount ?? DefaultMaxScreenshots, MaxScreenshots);
			var range = query.EndLocalTime - query.StartLocalTime;
			if (matching.Count > effectiveMax && range.TotalSeconds > 0)
			{
				interval = TimeSpan.FromSeconds(range.TotalSeconds / effectiveMax);
			}
		}

		if (interval is { } iv && iv > TimeSpan.Zero)
		{
			matching = ApplySampling(matching, iv);
		}

		return (matching, strategyUsed);
	}

	private ScreenshotSelection BuildListResult(
		List<ScreenshotInfo> matching, int totalMatching, SamplingStrategy strategyUsed, int? maxCount)
	{
		var effectiveLimit = Math.Min(maxCount ?? DefaultMaxScreenshots, MaxScreenshots);
		var isTruncated = matching.Count > effectiveLimit;
		if (isTruncated)
		{
			matching = matching.Take(effectiveLimit).ToList();
		}

		foreach (var screenshot in matching)
		{
			screenshot.Ref = _registry.Register(screenshot);
		}

		_logger.ScreenshotsSelected(matching.Count, totalMatching);

		return new ScreenshotSelection
		{
			Screenshots = matching.AsReadOnly(),
			TotalMatching = totalMatching,
			IsTruncated = isTruncated,
			SamplingStrategyUsed = strategyUsed,
		};
	}

	/// <inheritdoc />
	public byte[]? ReadScreenshot(string filePath)
	{
		// Security: validate the path is within the screenshot directory
		var screenshotDir = ResolveScreenshotDirectory();
		if (screenshotDir is null)
		{
			return null;
		}

		// Resolve to absolute paths for comparison
		var fullPath = Path.GetFullPath(filePath);
		var fullDir = Path.GetFullPath(screenshotDir);

		// Path traversal check
		if (!fullPath.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase))
		{
			_logger.ScreenshotPathTraversalBlocked(filePath);
			return null;
		}

		// Extension check
		if (!fullPath.EndsWith(JpgExtension, StringComparison.OrdinalIgnoreCase))
		{
			_logger.ScreenshotInvalidExtension(filePath);
			return null;
		}

		if (!File.Exists(fullPath))
		{
			return null;
		}

		try
		{
			return File.ReadAllBytes(fullPath);
		}
#pragma warning disable CA1031 // Do not catch general exception types — file may be locked or inaccessible
		catch (Exception ex)
#pragma warning restore CA1031
		{
			_logger.ScreenshotReadFailed(fullPath, ex);
			return null;
		}
	}

	/// <inheritdoc />
	public long WriteScreenshot(byte[] data, string outputPath, string allowedRootDirectory)
	{
		var fullPath = Path.GetFullPath(outputPath);
		var fullRoot = Path.GetFullPath(allowedRootDirectory);

		// Ensure trailing separator for proper prefix check
		if (!fullRoot.EndsWith(Path.DirectorySeparatorChar))
		{
			fullRoot += Path.DirectorySeparatorChar;
		}

		// Path traversal check
		if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
		{
			_logger.ScreenshotWritePathTraversalBlocked(outputPath);
			return -1;
		}

		// Extension check — only .jpg and .png allowed
		var ext = Path.GetExtension(fullPath);
		if (!ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
			&& !ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
			&& !ext.Equals(".png", StringComparison.OrdinalIgnoreCase))
		{
			_logger.ScreenshotWriteInvalidExtension(outputPath);
			return -1;
		}

		try
		{
			// Ensure directory exists
			var dir = Path.GetDirectoryName(fullPath);
			if (dir is not null && !Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			File.WriteAllBytes(fullPath, data);
			_logger.ScreenshotSaved(fullPath, data.Length);
			return data.Length;
		}
#pragma warning disable CA1031 // Do not catch general exception types — file write may fail for many reasons
		catch (Exception ex)
#pragma warning restore CA1031
		{
			_logger.ScreenshotWriteFailed(fullPath, ex);
			return -1;
		}
	}

	private string? ResolveScreenshotDirectory()
	{
		var result = _resolver.Resolve();
		return result.Path is not null
			? Path.Combine(result.Path, ScreenshotDirectoryName)
			: null;
	}

	private static List<ScreenshotInfo> ScanAndParse(string directory)
	{
		var results = new List<ScreenshotInfo>();

		try
		{
			foreach (var file in Directory.EnumerateFiles(directory, "*.jpg", SearchOption.AllDirectories))
			{
				var info = ScreenshotFilenameParser.TryParse(file);
				if (info is not null)
				{
					results.Add(info);
				}
			}
		}
#pragma warning disable CA1031 // Do not catch general exception types — directory enumeration may fail
		catch (Exception)
#pragma warning restore CA1031
		{
			// Return whatever we've collected so far
		}

		return results;
	}

	/// <summary>
	/// Groups screenshots by timestamp+sequence+monitor and picks the preferred variant
	/// (thumbnail or full-size). If the preferred variant is unavailable, falls back to the other.
	/// </summary>
	private static List<ScreenshotInfo> PreferVariant(List<ScreenshotInfo> screenshots, bool preferThumbnail)
	{
		return screenshots
			.GroupBy(s => (s.Date, s.Time, s.Sequence, s.Monitor))
			.Select(g =>
			{
				var preferred = g.FirstOrDefault(s => s.IsThumbnail == preferThumbnail);
				return preferred ?? g.First();
			})
			.OrderBy(s => s.LocalTimestamp)
			.ToList();
	}

	private static List<ScreenshotInfo> ApplySampling(List<ScreenshotInfo> screenshots, TimeSpan interval)
	{
		if (screenshots.Count == 0)
		{
			return screenshots;
		}

		var sampled = new List<ScreenshotInfo> { screenshots[0] };
		var lastTimestamp = screenshots[0].LocalTimestamp;

		for (var i = 1; i < screenshots.Count; i++)
		{
			if (screenshots[i].LocalTimestamp - lastTimestamp >= interval)
			{
				sampled.Add(screenshots[i]);
				lastTimestamp = screenshots[i].LocalTimestamp;
			}
		}

		return sampled;
	}

	private static ScreenshotSelection EmptySelection() => new()
	{
		Screenshots = [],
		TotalMatching = 0,
		IsTruncated = false,
	};
}
