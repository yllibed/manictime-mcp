using System.Globalization;

namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Parses ManicTime screenshot filenames using <see cref="ReadOnlySpan{T}"/>-based parsing.
/// Expected pattern: {date}_{time}_{offset}_{width}_{height}_{seq}_{monitor}[.thumbnail].jpg
/// Example: 2025-01-15_08-30-00_+02-00_1920_1080_0_0.jpg
/// Example: 2025-01-15_08-30-00_+02-00_1920_1080_0_0.thumbnail.jpg
/// </summary>
public static class ScreenshotFilenameParser
{
	private const string JpgExtension = ".jpg";
	private const string ThumbnailSuffix = ".thumbnail";

	/// <summary>
	/// Tries to parse a screenshot filename into its components.
	/// Returns null if the filename does not match the expected pattern.
	/// </summary>
	public static ScreenshotInfo? TryParse(string filePath)
	{
		var fileName = Path.GetFileName(filePath.AsSpan());
		if (fileName.IsEmpty)
		{
			return null;
		}

		return TryParseCore(fileName, filePath);
	}

	private static ScreenshotInfo? TryParseCore(ReadOnlySpan<char> fileName, string filePath)
	{
		if (!fileName.EndsWith(JpgExtension.AsSpan(), StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}

		var remaining = fileName[..^JpgExtension.Length];

		var isThumbnail = remaining.EndsWith(ThumbnailSuffix.AsSpan(), StringComparison.OrdinalIgnoreCase);
		if (isThumbnail)
		{
			remaining = remaining[..^ThumbnailSuffix.Length];
		}

		if (!TryParseDateSegment(ref remaining, out var date) ||
			!TryParseTimeSegment(ref remaining, out var time) ||
			!TryParseOffsetSegment(ref remaining, out var offset) ||
			!TryParseInt(ref remaining, out var width) ||
			!TryParseInt(ref remaining, out var height) ||
			!TryParseInt(ref remaining, out var seq) ||
			!TryParseLastInt(remaining, out var monitor))
		{
			return null;
		}

		return new ScreenshotInfo
		{
			Date = date,
			Time = time,
			Offset = offset,
			Width = width,
			Height = height,
			Sequence = seq,
			Monitor = monitor,
			IsThumbnail = isThumbnail,
			FilePath = filePath,
		};
	}

	private static bool TryParseDateSegment(ref ReadOnlySpan<char> remaining, out DateOnly date)
	{
		date = default;
		if (remaining.Length < 11 || remaining[10] != '_')
		{
			return false;
		}

		var dateSpan = remaining[..10];
		remaining = remaining[11..];
		return DateOnly.TryParseExact(dateSpan, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
	}

	private static bool TryParseTimeSegment(ref ReadOnlySpan<char> remaining, out TimeOnly time)
	{
		time = default;
		if (remaining.Length < 9 || remaining[8] != '_')
		{
			return false;
		}

		var timeSpan = remaining[..8];
		remaining = remaining[9..];
		return TryParseTime(timeSpan, out time);
	}

	private static bool TryParseOffsetSegment(ref ReadOnlySpan<char> remaining, out string offset)
	{
		offset = string.Empty;
		if (remaining.Length < 7 || remaining[6] != '_')
		{
			return false;
		}

		var offsetSpan = remaining[..6];
		remaining = remaining[7..];

		if ((offsetSpan[0] != '+' && offsetSpan[0] != '-') || offsetSpan[3] != '-')
		{
			return false;
		}

		offset = offsetSpan.ToString();
		return true;
	}

	private static bool TryParseTime(ReadOnlySpan<char> span, out TimeOnly time)
	{
		time = default;
		if (span.Length != 8 || span[2] != '-' || span[5] != '-')
		{
			return false;
		}

		if (!int.TryParse(span[..2], NumberStyles.None, CultureInfo.InvariantCulture, out var hours) ||
			!int.TryParse(span[3..5], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes) ||
			!int.TryParse(span[6..8], NumberStyles.None, CultureInfo.InvariantCulture, out var seconds))
		{
			return false;
		}

		if (hours is < 0 or > 23 || minutes is < 0 or > 59 || seconds is < 0 or > 59)
		{
			return false;
		}

		time = new TimeOnly(hours, minutes, seconds);
		return true;
	}

	private static bool TryParseInt(ref ReadOnlySpan<char> remaining, out int value)
	{
		value = 0;
		var underscoreIdx = remaining.IndexOf('_');
		if (underscoreIdx <= 0)
		{
			return false;
		}

		if (!int.TryParse(remaining[..underscoreIdx], NumberStyles.None, CultureInfo.InvariantCulture, out value))
		{
			return false;
		}

		remaining = remaining[(underscoreIdx + 1)..];
		return true;
	}

	private static bool TryParseLastInt(ReadOnlySpan<char> remaining, out int value) =>
		int.TryParse(remaining, NumberStyles.None, CultureInfo.InvariantCulture, out value);
}
