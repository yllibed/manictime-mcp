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
		InsertGroup(connection, groupId: 1, reportId: 2, name: "Visual Studio");
		InsertGroup(connection, groupId: 2, reportId: 2, name: "Chrome");
		InsertGroup(connection, groupId: 3, reportId: 2, name: "Terminal");

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
			name: "devenv.exe", groupId: 1);
		InsertActivity(connection, activityId: 4, reportId: 2,
			start: "2025-01-15 10:00:00", end: "2025-01-15 11:30:00",
			name: "chrome.exe", groupId: 2);
		InsertActivity(connection, activityId: 5, reportId: 2,
			start: "2025-01-15 11:30:00", end: "2025-01-15 12:00:00",
			name: "WindowsTerminal.exe", groupId: 3);
		InsertActivity(connection, activityId: 6, reportId: 2,
			start: "2025-01-15 13:00:00", end: "2025-01-15 17:30:00",
			name: "devenv.exe", groupId: 1);

		// Activities for Documents
		InsertActivity(connection, activityId: 7, reportId: 3,
			start: "2025-01-15 08:00:00", end: "2025-01-15 12:00:00",
			name: "Program.cs", groupId: 4);

		// Activities with null name and group
		InsertActivity(connection, activityId: 8, reportId: 1,
			start: "2025-01-15 12:00:00", end: "2025-01-15 13:00:00",
			name: null, groupId: null);
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

	private static void InsertGroup(SqliteConnection connection, long groupId, long reportId, string name)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_Group (GroupId, ReportId, Name) VALUES (@id, @report, @name)";
		command.Parameters.AddWithValue("@id", groupId);
		command.Parameters.AddWithValue("@report", reportId);
		command.Parameters.AddWithValue("@name", name);
		command.ExecuteNonQuery();
	}

	private static void InsertActivity(SqliteConnection connection, long activityId, long reportId, string start, string end, string? name, long? groupId)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_Activity (ActivityId, ReportId, StartLocalTime, EndLocalTime, Name, GroupId) VALUES (@id, @report, @start, @end, @name, @group)";
		command.Parameters.AddWithValue("@id", activityId);
		command.Parameters.AddWithValue("@report", reportId);
		command.Parameters.AddWithValue("@start", start);
		command.Parameters.AddWithValue("@end", end);
		command.Parameters.AddWithValue("@name", name is not null ? name : DBNull.Value);
		command.Parameters.AddWithValue("@group", groupId.HasValue ? groupId.Value : DBNull.Value);
		command.ExecuteNonQuery();
	}
}
