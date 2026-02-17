using System.Security.Cryptography;
using System.Text;

namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Creates opaque, deterministic screenshot references from filename components.
/// The ref is a URL-safe base64-encoded SHA256 hash (first 16 bytes) of the canonical key.
/// </summary>
public static class ScreenshotRef
{
	/// <summary>Creates a deterministic reference string from screenshot metadata.</summary>
	public static string Create(ScreenshotInfo info)
	{
		// Canonical key: date_time_offset_width_height_seq_monitor_thumbnail
		var key = string.Concat(
			info.Date.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture), "_",
			info.Time.ToString("HH-mm-ss", System.Globalization.CultureInfo.InvariantCulture), "_",
			info.Offset, "_",
			info.Width.ToString(System.Globalization.CultureInfo.InvariantCulture), "_",
			info.Height.ToString(System.Globalization.CultureInfo.InvariantCulture), "_",
			info.Sequence.ToString(System.Globalization.CultureInfo.InvariantCulture), "_",
			info.Monitor.ToString(System.Globalization.CultureInfo.InvariantCulture), "_",
			info.IsThumbnail.ToString());
		var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
		return Convert.ToBase64String(hash, offset: 0, length: 16)
			.Replace(oldChar: '+', newChar: '-')
			.Replace(oldChar: '/', newChar: '_')
			.TrimEnd('=');
	}
}
