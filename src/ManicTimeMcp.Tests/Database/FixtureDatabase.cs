using Microsoft.Data.Sqlite;

namespace ManicTimeMcp.Tests.Database;

/// <summary>
/// Creates minimal SQLite fixture databases for testing.
/// Databases are written to temp files and cleaned up on dispose.
/// </summary>
internal sealed class FixtureDatabase : IDisposable
{
	public string FilePath { get; }

	private FixtureDatabase(string filePath)
	{
		FilePath = filePath;
	}

	/// <summary>Creates a fixture DB with the standard ManicTime schema and optional seed data.</summary>
	public static FixtureDatabase CreateStandard(Action<SqliteConnection>? seedAction = null)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ManicTimeMcp_Test_{Guid.NewGuid():N}.db");
		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Mode = SqliteOpenMode.ReadWriteCreate,
		}.ToString();

		using var connection = new SqliteConnection(connectionString);
		connection.Open();

		CreateStandardSchema(connection);
		seedAction?.Invoke(connection);

		return new FixtureDatabase(path);
	}

	/// <summary>Creates a fixture DB with a specific subset of tables (for drift tests).</summary>
	public static FixtureDatabase CreatePartial(params string[] tablesToInclude)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ManicTimeMcp_Test_{Guid.NewGuid():N}.db");
		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Mode = SqliteOpenMode.ReadWriteCreate,
		}.ToString();

		using var connection = new SqliteConnection(connectionString);
		connection.Open();

		var tables = new HashSet<string>(tablesToInclude, StringComparer.OrdinalIgnoreCase);

		if (tables.Contains("Ar_Timeline"))
		{
			CreateTimelineTable(connection);
		}

		if (tables.Contains("Ar_Activity"))
		{
			CreateActivityTable(connection);
		}

		if (tables.Contains("Ar_Group"))
		{
			CreateGroupTable(connection);
		}

		return new FixtureDatabase(path);
	}

	/// <summary>Creates a fixture DB with a table that has missing columns (for drift tests).</summary>
	public static FixtureDatabase CreateWithMissingColumn(string tableName, string columnToOmit)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ManicTimeMcp_Test_{Guid.NewGuid():N}.db");
		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Mode = SqliteOpenMode.ReadWriteCreate,
		}.ToString();

		using var connection = new SqliteConnection(connectionString);
		connection.Open();

		// Create all tables but with one column missing from the specified table
		if (tableName.Equals("Ar_Timeline", StringComparison.OrdinalIgnoreCase))
		{
			CreateTimelineTableWithout(connection, columnToOmit);
		}
		else
		{
			CreateTimelineTable(connection);
		}

		if (tableName.Equals("Ar_Activity", StringComparison.OrdinalIgnoreCase))
		{
			CreateActivityTableWithout(connection, columnToOmit);
		}
		else
		{
			CreateActivityTable(connection);
		}

		if (tableName.Equals("Ar_Group", StringComparison.OrdinalIgnoreCase))
		{
			CreateGroupTableWithout(connection, columnToOmit);
		}
		else
		{
			CreateGroupTable(connection);
		}

		return new FixtureDatabase(path);
	}

	public void Dispose()
	{
		try
		{
			if (File.Exists(FilePath))
			{
				File.Delete(FilePath);
			}
		}
#pragma warning disable CA1031 // Do not catch general exception types — cleanup best effort
		catch (Exception)
#pragma warning restore CA1031
		{
			// Best effort cleanup
		}
	}

	private static void CreateStandardSchema(SqliteConnection connection)
	{
		CreateTimelineTable(connection);
		CreateActivityTable(connection);
		CreateGroupTable(connection);
	}

	private static void CreateTimelineTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_Timeline (
				ReportId INTEGER PRIMARY KEY,
				SchemaName TEXT NOT NULL,
				BaseSchemaName TEXT NOT NULL
			)
			""");
	}

	private static void CreateActivityTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_Activity (
				ActivityId INTEGER PRIMARY KEY,
				TimelineId INTEGER NOT NULL,
				StartLocalTime TEXT NOT NULL,
				EndLocalTime TEXT NOT NULL,
				DisplayName TEXT,
				GroupId INTEGER,
				FOREIGN KEY (TimelineId) REFERENCES Ar_Timeline(ReportId)
			)
			""");
	}

	private static void CreateGroupTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_Group (
				GroupId INTEGER PRIMARY KEY,
				TimelineId INTEGER NOT NULL,
				DisplayName TEXT NOT NULL,
				ParentGroupId INTEGER,
				FOREIGN KEY (TimelineId) REFERENCES Ar_Timeline(ReportId)
			)
			""");
	}

	private static void CreateTimelineTableWithout(SqliteConnection connection, string columnToOmit)
	{
		var columns = new List<string> { "ReportId INTEGER PRIMARY KEY", "SchemaName TEXT NOT NULL", "BaseSchemaName TEXT NOT NULL" };
		columns.RemoveAll(c => c.StartsWith(columnToOmit, StringComparison.OrdinalIgnoreCase));
		Execute(connection, $"CREATE TABLE Ar_Timeline ({string.Join(", ", columns)})");
	}

	private static void CreateActivityTableWithout(SqliteConnection connection, string columnToOmit)
	{
		var columns = new List<string>
		{
			"ActivityId INTEGER PRIMARY KEY",
			"TimelineId INTEGER NOT NULL",
			"StartLocalTime TEXT NOT NULL",
			"EndLocalTime TEXT NOT NULL",
			"DisplayName TEXT",
			"GroupId INTEGER",
		};
		columns.RemoveAll(c => c.StartsWith(columnToOmit, StringComparison.OrdinalIgnoreCase));
		Execute(connection, $"CREATE TABLE Ar_Activity ({string.Join(", ", columns)})");
	}

	private static void CreateGroupTableWithout(SqliteConnection connection, string columnToOmit)
	{
		var columns = new List<string>
		{
			"GroupId INTEGER PRIMARY KEY",
			"TimelineId INTEGER NOT NULL",
			"DisplayName TEXT NOT NULL",
			"ParentGroupId INTEGER",
		};
		columns.RemoveAll(c => c.StartsWith(columnToOmit, StringComparison.OrdinalIgnoreCase));
		Execute(connection, $"CREATE TABLE Ar_Group ({string.Join(", ", columns)})");
	}

	private static void Execute(SqliteConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}
}
