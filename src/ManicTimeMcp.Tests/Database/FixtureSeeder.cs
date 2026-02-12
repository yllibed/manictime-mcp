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
		InsertGroup(connection, groupId: 1, timelineId: 2, displayName: "Visual Studio", parentGroupId: null);
		InsertGroup(connection, groupId: 2, timelineId: 2, displayName: "Chrome", parentGroupId: null);
		InsertGroup(connection, groupId: 3, timelineId: 2, displayName: "Terminal", parentGroupId: null);

		// Groups for Documents timeline
		InsertGroup(connection, groupId: 4, timelineId: 3, displayName: "Project.sln", parentGroupId: null);

		// Activities for Computer Usage
		InsertActivity(connection, activityId: 1, timelineId: 1,
			start: "2025-01-15 08:00:00", end: "2025-01-15 12:00:00",
			displayName: "Using Computer", groupId: null);
		InsertActivity(connection, activityId: 2, timelineId: 1,
			start: "2025-01-15 13:00:00", end: "2025-01-15 17:30:00",
			displayName: "Using Computer", groupId: null);

		// Activities for Applications
		InsertActivity(connection, activityId: 3, timelineId: 2,
			start: "2025-01-15 08:00:00", end: "2025-01-15 10:00:00",
			displayName: "devenv.exe", groupId: 1);
		InsertActivity(connection, activityId: 4, timelineId: 2,
			start: "2025-01-15 10:00:00", end: "2025-01-15 11:30:00",
			displayName: "chrome.exe", groupId: 2);
		InsertActivity(connection, activityId: 5, timelineId: 2,
			start: "2025-01-15 11:30:00", end: "2025-01-15 12:00:00",
			displayName: "WindowsTerminal.exe", groupId: 3);
		InsertActivity(connection, activityId: 6, timelineId: 2,
			start: "2025-01-15 13:00:00", end: "2025-01-15 17:30:00",
			displayName: "devenv.exe", groupId: 1);

		// Activities for Documents
		InsertActivity(connection, activityId: 7, timelineId: 3,
			start: "2025-01-15 08:00:00", end: "2025-01-15 12:00:00",
			displayName: "Program.cs", groupId: 4);

		// Activities with null display name and group
		InsertActivity(connection, activityId: 8, timelineId: 1,
			start: "2025-01-15 12:00:00", end: "2025-01-15 13:00:00",
			displayName: null, groupId: null);
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

	private static void InsertGroup(SqliteConnection connection, long groupId, long timelineId, string displayName, long? parentGroupId)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_Group (GroupId, TimelineId, DisplayName, ParentGroupId) VALUES (@id, @timeline, @name, @parent)";
		command.Parameters.AddWithValue("@id", groupId);
		command.Parameters.AddWithValue("@timeline", timelineId);
		command.Parameters.AddWithValue("@name", displayName);
		command.Parameters.AddWithValue("@parent", parentGroupId.HasValue ? parentGroupId.Value : DBNull.Value);
		command.ExecuteNonQuery();
	}

	private static void InsertActivity(SqliteConnection connection, long activityId, long timelineId, string start, string end, string? displayName, long? groupId)
	{
		using var command = connection.CreateCommand();
		command.CommandText = "INSERT INTO Ar_Activity (ActivityId, TimelineId, StartLocalTime, EndLocalTime, DisplayName, GroupId) VALUES (@id, @timeline, @start, @end, @name, @group)";
		command.Parameters.AddWithValue("@id", activityId);
		command.Parameters.AddWithValue("@timeline", timelineId);
		command.Parameters.AddWithValue("@start", start);
		command.Parameters.AddWithValue("@end", end);
		command.Parameters.AddWithValue("@name", displayName is not null ? displayName : DBNull.Value);
		command.Parameters.AddWithValue("@group", groupId.HasValue ? groupId.Value : DBNull.Value);
		command.ExecuteNonQuery();
	}
}
