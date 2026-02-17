using Microsoft.Data.Sqlite;

namespace ManicTimeMcp.Tests.Database;

/// <summary>Seeds fixture databases with synthetic ManicTime data.</summary>
internal static class FixtureSeeder
{
	/// <summary>Inserts a standard set of timelines, activities, and groups.</summary>
	public static void SeedStandardData(SqliteConnection connection)
	{
		// Timelines
		InsertTimeline(connection, reportId: 1, schemaName: "ManicTime/ComputerUsage", baseSchemaName: "ManicTime/ComputerUsage");
		InsertTimeline(connection, reportId: 2, schemaName: "ManicTime/Applications", baseSchemaName: "ManicTime/Applications");
		InsertTimeline(connection, reportId: 3, schemaName: "ManicTime/Documents", baseSchemaName: "ManicTime/Documents");
		InsertTimeline(connection, reportId: 4, schemaName: "ManicTime/Tags", baseSchemaName: "ManicTime/Tags");

		// Groups for Applications timeline
		InsertGroup(connection, groupId: 1, reportId: 2, name: "Visual Studio", color: "#FF0000", key: "devenv.exe");
		InsertGroup(connection, groupId: 2, reportId: 2, name: "Chrome", color: "#00FF00", key: "chrome.exe");
		InsertGroup(connection, groupId: 3, reportId: 2, name: "Terminal", color: "#0000FF", key: "WindowsTerminal.exe");

		// Groups for Documents timeline
		InsertGroup(connection, groupId: 4, reportId: 3, name: "Project.sln");

		// Activities for Computer Usage
		InsertActivity(connection, activityId: 1, reportId: 1,
			start: "2025-01-15 08:00:00", end: "2025-01-15 12:00:00",
			name: "Using Computer", groupId: null);
		InsertActivity(connection, activityId: 2, reportId: 1,
			start: "2025-01-15 13:00:00", end: "2025-01-15 17:30:00",
			name: "Using Computer", groupId: null);

		// Activities for Applications
		InsertActivity(connection, activityId: 3, reportId: 2,
			start: "2025-01-15 08:00:00", end: "2025-01-15 10:00:00",
			name: "devenv.exe", groupId: 1, commonGroupId: 1);
		InsertActivity(connection, activityId: 4, reportId: 2,
			start: "2025-01-15 10:00:00", end: "2025-01-15 11:30:00",
			name: "chrome.exe", groupId: 2, commonGroupId: 2);
		InsertActivity(connection, activityId: 5, reportId: 2,
			start: "2025-01-15 11:30:00", end: "2025-01-15 12:00:00",
			name: "WindowsTerminal.exe", groupId: 3, commonGroupId: 3);
		InsertActivity(connection, activityId: 6, reportId: 2,
			start: "2025-01-15 13:00:00", end: "2025-01-15 17:30:00",
			name: "devenv.exe", groupId: 1, commonGroupId: 1);

		// Activities for Documents
		InsertActivity(connection, activityId: 7, reportId: 3,
			start: "2025-01-15 08:00:00", end: "2025-01-15 12:00:00",
			name: "Program.cs", groupId: 4);

		// Activities with null name and group
		InsertActivity(connection, activityId: 8, reportId: 1,
			start: "2025-01-15 12:00:00", end: "2025-01-15 13:00:00",
			name: null, groupId: null);
	}

	/// <summary>Seeds supplemental pre-aggregated data (common groups, daily/hourly usage, etc.).</summary>
	public static void SeedSupplementalData(SqliteConnection connection)
	{
		// Common groups
		InsertCommonGroup(connection, commonGroupId: 1, name: "Visual Studio", color: "#FF0000", key: "devenv.exe");
		InsertCommonGroup(connection, commonGroupId: 2, name: "Chrome", color: "#00FF00", key: "chrome.exe");
		InsertCommonGroup(connection, commonGroupId: 3, name: "Terminal", color: "#0000FF", key: "WindowsTerminal.exe");

		// Application by day
		InsertDailyUsage(connection, "Ar_ApplicationByDay", "2025-01-15", commonGroupId: 1, totalSeconds: 7200);
		InsertDailyUsage(connection, "Ar_ApplicationByDay", "2025-01-15", commonGroupId: 2, totalSeconds: 5400);
		InsertDailyUsage(connection, "Ar_ApplicationByDay", "2025-01-15", commonGroupId: 3, totalSeconds: 1800);
		InsertDailyUsage(connection, "Ar_ApplicationByDay", "2025-01-16", commonGroupId: 1, totalSeconds: 3600);

		// Website by day
		InsertDailyUsage(connection, "Ar_WebSiteByDay", "2025-01-15", commonGroupId: 2, totalSeconds: 3600);

		// Document by day
		InsertDailyUsage(connection, "Ar_DocumentByDay", "2025-01-15", commonGroupId: 1, totalSeconds: 14400);

		// Application by year
		InsertDailyUsage(connection, "Ar_ApplicationByYear", "2025-01-15", commonGroupId: 1, totalSeconds: 7200);

		// Activity by hour
		InsertHourlyUsage(connection, "2025-01-15", hour: 8, commonGroupId: 1, totalSeconds: 3600);
		InsertHourlyUsage(connection, "2025-01-15", hour: 9, commonGroupId: 1, totalSeconds: 3600);
		InsertHourlyUsage(connection, "2025-01-15", hour: 10, commonGroupId: 2, totalSeconds: 3600);

		// Timeline summary
		InsertTimelineSummary(connection, reportId: 1, start: "2025-01-15 08:00:00", end: "2025-01-15 17:30:00");
		InsertTimelineSummary(connection, reportId: 2, start: "2025-01-15 08:00:00", end: "2025-01-15 17:30:00");

		// Environment
		InsertEnvironment(connection, environmentId: 1, deviceName: "WORKSTATION-01");

		// Tags
		InsertTag(connection, tagId: 1, name: "coding");
		InsertTag(connection, tagId: 2, name: "browsing");
		InsertActivityTag(connection, activityId: 3, tagId: 1);
		InsertActivityTag(connection, activityId: 4, tagId: 2);
	}

	/// <summary>Seeds both standard and supplemental data.</summary>
	public static void SeedFullData(SqliteConnection connection)
	{
		SeedStandardData(connection);
		SeedSupplementalData(connection);
	}

	/// <summary>Seeds an activity that crosses midnight (23:30 to 00:30) for boundary-split tests.</summary>
	public static void SeedCrossMidnightData(SqliteConnection connection)
	{
		InsertTimeline(connection, reportId: 1, schemaName: "ManicTime/Applications", baseSchemaName: "ManicTime/Applications");
		InsertGroup(connection, groupId: 1, reportId: 1, name: "VS Code", color: "#007ACC", key: "code.exe");

		// 30 min before midnight + 30 min after midnight = 60 min total
		InsertActivity(connection, activityId: 1, reportId: 1,
			start: "2025-01-15 23:30:00", end: "2025-01-16 00:30:00",
			name: "code.exe", groupId: 1);

		// Activity that crosses an hour boundary: 08:45 - 09:15 = 30 min total (15 in h8, 15 in h9)
		InsertActivity(connection, activityId: 2, reportId: 1,
			start: "2025-01-15 08:45:00", end: "2025-01-15 09:15:00",
			name: "code.exe", groupId: 1);
	}

	/// <summary>Seeds data with GenericGroup base schema matching real ManicTime databases.</summary>
	public static void SeedGenericGroupSchemaData(SqliteConnection connection)
	{
		// Real ManicTime uses BaseSchemaName = "ManicTime/GenericGroup" for Applications/Documents/BrowserUrls
		InsertTimeline(connection, reportId: 1, schemaName: "ManicTime/Applications", baseSchemaName: "ManicTime/GenericGroup");
		InsertTimeline(connection, reportId: 2, schemaName: "ManicTime/Documents", baseSchemaName: "ManicTime/GenericGroup");

		// Groups for Applications timeline
		InsertGroup(connection, groupId: 1, reportId: 1, name: "VS Code", color: "#007ACC", key: "code.exe");
		InsertGroup(connection, groupId: 2, reportId: 1, name: "Firefox", color: "#FF7139", key: "firefox.exe");

		// Activities for Applications
		InsertActivity(connection, activityId: 1, reportId: 1,
			start: "2025-01-15 08:00:00", end: "2025-01-15 10:00:00",
			name: "code.exe", groupId: 1);
		InsertActivity(connection, activityId: 2, reportId: 1,
			start: "2025-01-15 10:00:00", end: "2025-01-15 12:00:00",
			name: "firefox.exe", groupId: 2);

		// Activities for Documents
		InsertActivity(connection, activityId: 3, reportId: 2,
			start: "2025-01-15 08:00:00", end: "2025-01-15 10:00:00",
			name: "README.md", groupId: null);
	}

	/// <summary>
	/// Seeds data where GroupId values overlap across timelines,
	/// reproducing the real ManicTime DB scenario that causes row multiplication
	/// when GROUP JOINs lack the ReportId scope.
	/// </summary>
	public static void SeedOverlappingGroupIdData(SqliteConnection connection)
	{
		// Two timelines that both have GroupId=1
		InsertTimeline(connection, reportId: 1, schemaName: "ManicTime/Applications", baseSchemaName: "ManicTime/GenericGroup");
		InsertTimeline(connection, reportId: 2, schemaName: "ManicTime/Documents", baseSchemaName: "ManicTime/GenericGroup");

		// Same GroupId (1) in both timelines — this is how real ManicTime DBs work
		InsertGroup(connection, groupId: 1, reportId: 1, name: "VS Code", color: "#007ACC", key: "code.exe");
		InsertGroup(connection, groupId: 1, reportId: 2, name: "Project Files", color: "#FFD700", key: null);

		// Different GroupId unique to each timeline
		InsertGroup(connection, groupId: 2, reportId: 1, name: "Firefox", color: "#FF7139", key: "firefox.exe");
		InsertGroup(connection, groupId: 2, reportId: 2, name: "Docs Folder", color: "#808080", key: null);

		// Activities on timeline 1 (Applications)
		InsertActivity(connection, activityId: 1, reportId: 1,
			start: "2025-01-15 08:00:00", end: "2025-01-15 10:00:00",
			name: "code.exe", groupId: 1, commonGroupId: 1);
		InsertActivity(connection, activityId: 2, reportId: 1,
			start: "2025-01-15 10:00:00", end: "2025-01-15 12:00:00",
			name: "firefox.exe", groupId: 2, commonGroupId: 2);

		// Activities on timeline 2 (Documents)
		InsertActivity(connection, activityId: 3, reportId: 2,
			start: "2025-01-15 08:00:00", end: "2025-01-15 11:00:00",
			name: "README.md", groupId: 1);
		InsertActivity(connection, activityId: 4, reportId: 2,
			start: "2025-01-15 11:00:00", end: "2025-01-15 12:00:00",
			name: "design.docx", groupId: 2);
	}

	private static void InsertTimeline(SqliteConnection connection, long reportId, string schemaName, string baseSchemaName)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_Timeline (ReportId, SchemaName, BaseSchemaName) VALUES (@id, @schema, @base)";
		command.Parameters.AddWithValue("@id", reportId);
		command.Parameters.AddWithValue("@schema", schemaName);
		command.Parameters.AddWithValue("@base", baseSchemaName);
		command.ExecuteNonQuery();
	}

	private static void InsertGroup(
		SqliteConnection connection, long groupId, long reportId, string name, string? color = null, string? key = null)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_Group (GroupId, ReportId, Name, Color, Key) VALUES (@id, @report, @name, @color, @key)";
		command.Parameters.AddWithValue("@id", groupId);
		command.Parameters.AddWithValue("@report", reportId);
		command.Parameters.AddWithValue("@name", name);
		command.Parameters.AddWithValue("@color", color is not null ? color : DBNull.Value);
		command.Parameters.AddWithValue("@key", key is not null ? key : DBNull.Value);
		command.ExecuteNonQuery();
	}

	private static void InsertActivity(
		SqliteConnection connection, long activityId, long reportId, string start, string end,
		string? name, long? groupId, long? commonGroupId = null)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_Activity (ActivityId, ReportId, StartLocalTime, EndLocalTime, Name, GroupId, CommonGroupId) VALUES (@id, @report, @start, @end, @name, @group, @cgId)";
		command.Parameters.AddWithValue("@id", activityId);
		command.Parameters.AddWithValue("@report", reportId);
		command.Parameters.AddWithValue("@start", start);
		command.Parameters.AddWithValue("@end", end);
		command.Parameters.AddWithValue("@name", name is not null ? name : DBNull.Value);
		command.Parameters.AddWithValue("@group", groupId.HasValue ? groupId.Value : DBNull.Value);
		command.Parameters.AddWithValue("@cgId", commonGroupId.HasValue ? commonGroupId.Value : DBNull.Value);
		command.ExecuteNonQuery();
	}

	private static void InsertCommonGroup(SqliteConnection connection, long commonGroupId, string name, string? color, string? key)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_CommonGroup (CommonGroupId, Name, Color, Key) VALUES (@id, @name, @color, @key)";
		command.Parameters.AddWithValue("@id", commonGroupId);
		command.Parameters.AddWithValue("@name", name);
		command.Parameters.AddWithValue("@color", color is not null ? color : DBNull.Value);
		command.Parameters.AddWithValue("@key", key is not null ? key : DBNull.Value);
		command.ExecuteNonQuery();
	}

	private static void InsertDailyUsage(SqliteConnection connection, string tableName, string day, long commonGroupId, double totalSeconds)
	{
		using var command = connection.CreateCommand();
		command.CommandText = $"INSERT INTO {tableName} (Day, CommonGroupId, TotalSeconds) VALUES (@day, @cgId, @total)";
		command.Parameters.AddWithValue("@day", day);
		command.Parameters.AddWithValue("@cgId", commonGroupId);
		command.Parameters.AddWithValue("@total", totalSeconds);
		command.ExecuteNonQuery();
	}

	private static void InsertHourlyUsage(SqliteConnection connection, string day, int hour, long commonGroupId, double totalSeconds)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_ActivityByHour (Day, Hour, CommonGroupId, TotalSeconds) VALUES (@day, @hour, @cgId, @total)";
		command.Parameters.AddWithValue("@day", day);
		command.Parameters.AddWithValue("@hour", hour);
		command.Parameters.AddWithValue("@cgId", commonGroupId);
		command.Parameters.AddWithValue("@total", totalSeconds);
		command.ExecuteNonQuery();
	}

	private static void InsertTimelineSummary(SqliteConnection connection, long reportId, string start, string end)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_TimelineSummary (ReportId, StartLocalTime, EndLocalTime) VALUES (@id, @start, @end)";
		command.Parameters.AddWithValue("@id", reportId);
		command.Parameters.AddWithValue("@start", start);
		command.Parameters.AddWithValue("@end", end);
		command.ExecuteNonQuery();
	}

	private static void InsertEnvironment(SqliteConnection connection, long environmentId, string deviceName)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_Environment (EnvironmentId, DeviceName) VALUES (@id, @name)";
		command.Parameters.AddWithValue("@id", environmentId);
		command.Parameters.AddWithValue("@name", deviceName);
		command.ExecuteNonQuery();
	}

	internal static void InsertTag(SqliteConnection connection, long tagId, string name)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_Tag (TagId, Name) VALUES (@id, @name)";
		command.Parameters.AddWithValue("@id", tagId);
		command.Parameters.AddWithValue("@name", name);
		command.ExecuteNonQuery();
	}

	internal static void InsertActivityTag(SqliteConnection connection, long activityId, long tagId)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_ActivityTag (ActivityId, TagId) VALUES (@actId, @tagId)";
		command.Parameters.AddWithValue("@actId", activityId);
		command.Parameters.AddWithValue("@tagId", tagId);
		command.ExecuteNonQuery();
	}
}
