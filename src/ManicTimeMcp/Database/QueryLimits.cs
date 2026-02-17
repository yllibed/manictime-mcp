namespace ManicTimeMcp.Database;

/// <summary>Server-enforced hard caps for query results regardless of caller input.</summary>
public static class QueryLimits
{
	/// <summary>Maximum number of activities returned per query.</summary>
	public const int MaxActivities = 5000;

	/// <summary>Maximum number of timelines returned per query.</summary>
	public const int MaxTimelines = 200;

	/// <summary>Maximum number of groups returned per query.</summary>
	public const int MaxGroups = 1000;

	/// <summary>Default number of activities when no limit is specified.</summary>
	public const int DefaultActivities = 1000;

	/// <summary>Maximum number of hourly usage rows returned per query.</summary>
	public const int MaxHourlyUsageRows = 5000;

	/// <summary>Maximum number of daily usage rows returned per query.</summary>
	public const int MaxDailyUsageRows = 2000;

	/// <summary>Default number of usage rows when no limit is specified.</summary>
	public const int DefaultUsageLimit = 1000;

	/// <summary>Clamps a caller-supplied limit to the hard cap for the given maximum.</summary>
	public static int Clamp(int? requested, int defaultLimit, int hardCap) =>
		Math.Min(requested ?? defaultLimit, hardCap);
}
