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

		CreateCoreSchema(connection);
		seedAction?.Invoke(connection);

		return new FixtureDatabase(path);
	}

	/// <summary>Creates a fixture DB with all core + supplemental + informational tables.</summary>
	public static FixtureDatabase CreateFull(Action<SqliteConnection>? seedAction = null)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ManicTimeMcp_Test_{Guid.NewGuid():N}.db");
		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Mode = SqliteOpenMode.ReadWriteCreate,
		}.ToString();

		using var connection = new SqliteConnection(connectionString);
		connection.Open();

		CreateCoreSchema(connection);
		CreateSupplementalSchema(connection);
		CreateInformationalSchema(connection);
		seedAction?.Invoke(connection);

		return new FixtureDatabase(path);
	}

	/// <summary>Creates a fixture DB with core tables only (no supplemental/informational).</summary>
	public static FixtureDatabase CreateCoreOnly(Action<SqliteConnection>? seedAction = null)
	{
		return CreateStandard(seedAction);
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

		var requested = new HashSet<string>(tablesToInclude, StringComparer.OrdinalIgnoreCase);
		foreach (var (name, creator) in TableCreators)
		{
			if (requested.Contains(name))
			{
				creator(connection);
			}
		}

		return new FixtureDatabase(path);
	}

	private static readonly KeyValuePair<string, Action<SqliteConnection>>[] TableCreators =
	[
		new("Ar_Timeline", CreateTimelineTable),
		new("Ar_Activity", CreateActivityTable),
		new("Ar_Group", CreateGroupTable),
		new("Ar_CommonGroup", CreateCommonGroupTable),
		new("Ar_ApplicationByDay", CreateApplicationByDayTable),
		new("Ar_WebSiteByDay", CreateWebSiteByDayTable),
		new("Ar_DocumentByDay", CreateDocumentByDayTable),
		new("Ar_ApplicationByYear", CreateApplicationByYearTable),
		new("Ar_WebSiteByYear", CreateWebSiteByYearTable),
		new("Ar_DocumentByYear", CreateDocumentByYearTable),
		new("Ar_ActivityByHour", CreateActivityByHourTable),
		new("Ar_TimelineSummary", CreateTimelineSummaryTable),
		new("Ar_Environment", CreateEnvironmentTable),
		new("Ar_Folder", CreateFolderTable),
		new("Ar_Tag", CreateTagTable),
		new("Ar_ActivityTag", CreateActivityTagTable),
		new("Ar_Category", CreateCategoryTable),
		new("Ar_CategoryGroup", CreateCategoryGroupTable),
	];

	/// <summary>Creates a full fixture DB but with a specific column missing from one table.</summary>
	public static FixtureDatabase CreateFullWithMissingColumn(string tableName, string columnToOmit)
	{
		var path = Path.Combine(Path.GetTempPath(), $"ManicTimeMcp_Test_{Guid.NewGuid():N}.db");
		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = path,
			Mode = SqliteOpenMode.ReadWriteCreate,
		}.ToString();

		using var connection = new SqliteConnection(connectionString);
		connection.Open();

		// Create all tables from the creators, but replace the specified table with a column-omitted version
		var requested = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var (name, creator) in TableCreators)
		{
			if (!name.Equals(tableName, StringComparison.OrdinalIgnoreCase))
			{
				creator(connection);
			}
			requested.Add(name);
		}

		// Create the target table with missing column
		if (requested.Contains(tableName))
		{
			CreateTableWithout(connection, tableName, columnToOmit);
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

	// ── Core tables ──

	private static void CreateCoreSchema(SqliteConnection connection)
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
				ReportId INTEGER NOT NULL,
				StartLocalTime TEXT NOT NULL,
				EndLocalTime TEXT NOT NULL,
				Name TEXT,
				GroupId INTEGER,
				Notes TEXT,
				IsActive INTEGER DEFAULT 1,
				CommonGroupId INTEGER,
				StartUtcTime TEXT,
				EndUtcTime TEXT,
				FOREIGN KEY (ReportId) REFERENCES Ar_Timeline(ReportId)
			)
			""");
	}

	private static void CreateGroupTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_Group (
				GroupId INTEGER NOT NULL,
				ReportId INTEGER NOT NULL,
				Name TEXT NOT NULL,
				Color TEXT,
				Key TEXT,
				CommonId INTEGER,
				GroupType TEXT,
				PRIMARY KEY (GroupId, ReportId),
				FOREIGN KEY (ReportId) REFERENCES Ar_Timeline(ReportId)
			)
			""");
	}

	// ── Supplemental tables ──

	private static void CreateSupplementalSchema(SqliteConnection connection)
	{
		CreateCommonGroupTable(connection);
		CreateApplicationByDayTable(connection);
		CreateWebSiteByDayTable(connection);
		CreateDocumentByDayTable(connection);
		CreateApplicationByYearTable(connection);
		CreateWebSiteByYearTable(connection);
		CreateDocumentByYearTable(connection);
		CreateActivityByHourTable(connection);
		CreateTimelineSummaryTable(connection);
		CreateEnvironmentTable(connection);
		CreateFolderTable(connection);
		CreateTagTable(connection);
		CreateActivityTagTable(connection);
	}

	private static void CreateCommonGroupTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_CommonGroup (
				CommonGroupId INTEGER PRIMARY KEY,
				Name TEXT NOT NULL,
				Color TEXT,
				Key TEXT
			)
			""");
	}

	private static void CreateApplicationByDayTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_ApplicationByDay (
				Day TEXT NOT NULL,
				CommonGroupId INTEGER NOT NULL,
				TotalSeconds REAL NOT NULL
			)
			""");
	}

	private static void CreateWebSiteByDayTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_WebSiteByDay (
				Day TEXT NOT NULL,
				CommonGroupId INTEGER NOT NULL,
				TotalSeconds REAL NOT NULL
			)
			""");
	}

	private static void CreateDocumentByDayTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_DocumentByDay (
				Day TEXT NOT NULL,
				CommonGroupId INTEGER NOT NULL,
				TotalSeconds REAL NOT NULL
			)
			""");
	}

	private static void CreateApplicationByYearTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_ApplicationByYear (
				Day TEXT NOT NULL,
				CommonGroupId INTEGER NOT NULL,
				TotalSeconds REAL NOT NULL
			)
			""");
	}

	private static void CreateWebSiteByYearTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_WebSiteByYear (
				Day TEXT NOT NULL,
				CommonGroupId INTEGER NOT NULL,
				TotalSeconds REAL NOT NULL
			)
			""");
	}

	private static void CreateDocumentByYearTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_DocumentByYear (
				Day TEXT NOT NULL,
				CommonGroupId INTEGER NOT NULL,
				TotalSeconds REAL NOT NULL
			)
			""");
	}

	private static void CreateActivityByHourTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_ActivityByHour (
				Day TEXT NOT NULL,
				Hour INTEGER NOT NULL,
				CommonGroupId INTEGER NOT NULL,
				TotalSeconds REAL NOT NULL
			)
			""");
	}

	private static void CreateTimelineSummaryTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_TimelineSummary (
				ReportId INTEGER NOT NULL,
				StartLocalTime TEXT NOT NULL,
				EndLocalTime TEXT NOT NULL
			)
			""");
	}

	private static void CreateEnvironmentTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_Environment (
				EnvironmentId INTEGER PRIMARY KEY,
				DeviceName TEXT NOT NULL
			)
			""");
	}

	private static void CreateFolderTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_Folder (
				FolderId INTEGER PRIMARY KEY,
				Name TEXT NOT NULL
			)
			""");
	}

	private static void CreateTagTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_Tag (
				TagId INTEGER PRIMARY KEY,
				Name TEXT NOT NULL
			)
			""");
	}

	private static void CreateActivityTagTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_ActivityTag (
				ActivityId INTEGER NOT NULL,
				TagId INTEGER NOT NULL
			)
			""");
	}

	// ── Informational tables ──

	private static void CreateInformationalSchema(SqliteConnection connection)
	{
		CreateCategoryTable(connection);
		CreateCategoryGroupTable(connection);
	}

	private static void CreateCategoryTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_Category (
				CategoryId INTEGER PRIMARY KEY,
				Name TEXT NOT NULL
			)
			""");
	}

	private static void CreateCategoryGroupTable(SqliteConnection connection)
	{
		Execute(connection, """
			CREATE TABLE Ar_CategoryGroup (
				CategoryGroupId INTEGER PRIMARY KEY,
				Name TEXT NOT NULL
			)
			""");
	}

	// ── Drift-test helpers ──

	private static readonly Dictionary<string, string[]> TableColumnDefinitions = new(StringComparer.OrdinalIgnoreCase)
	{
		["Ar_Timeline"] = ["ReportId INTEGER PRIMARY KEY", "SchemaName TEXT NOT NULL", "BaseSchemaName TEXT NOT NULL"],
		["Ar_Activity"] = ["ActivityId INTEGER PRIMARY KEY", "ReportId INTEGER NOT NULL", "StartLocalTime TEXT NOT NULL", "EndLocalTime TEXT NOT NULL", "Name TEXT", "GroupId INTEGER", "Notes TEXT", "IsActive INTEGER DEFAULT 1", "CommonGroupId INTEGER", "StartUtcTime TEXT", "EndUtcTime TEXT"],
		["Ar_Group"] = ["GroupId INTEGER NOT NULL", "ReportId INTEGER NOT NULL", "Name TEXT NOT NULL", "Color TEXT", "Key TEXT", "CommonId INTEGER", "GroupType TEXT"],
		["Ar_CommonGroup"] = ["CommonGroupId INTEGER PRIMARY KEY", "Name TEXT NOT NULL", "Color TEXT", "Key TEXT"],
		["Ar_ApplicationByDay"] = ["Day TEXT NOT NULL", "CommonGroupId INTEGER NOT NULL", "TotalSeconds REAL NOT NULL"],
		["Ar_WebSiteByDay"] = ["Day TEXT NOT NULL", "CommonGroupId INTEGER NOT NULL", "TotalSeconds REAL NOT NULL"],
		["Ar_DocumentByDay"] = ["Day TEXT NOT NULL", "CommonGroupId INTEGER NOT NULL", "TotalSeconds REAL NOT NULL"],
		["Ar_ApplicationByYear"] = ["Day TEXT NOT NULL", "CommonGroupId INTEGER NOT NULL", "TotalSeconds REAL NOT NULL"],
		["Ar_WebSiteByYear"] = ["Day TEXT NOT NULL", "CommonGroupId INTEGER NOT NULL", "TotalSeconds REAL NOT NULL"],
		["Ar_DocumentByYear"] = ["Day TEXT NOT NULL", "CommonGroupId INTEGER NOT NULL", "TotalSeconds REAL NOT NULL"],
		["Ar_ActivityByHour"] = ["Day TEXT NOT NULL", "Hour INTEGER NOT NULL", "CommonGroupId INTEGER NOT NULL", "TotalSeconds REAL NOT NULL"],
		["Ar_TimelineSummary"] = ["ReportId INTEGER NOT NULL", "StartLocalTime TEXT NOT NULL", "EndLocalTime TEXT NOT NULL"],
		["Ar_Environment"] = ["EnvironmentId INTEGER PRIMARY KEY", "DeviceName TEXT NOT NULL"],
		["Ar_Folder"] = ["FolderId INTEGER PRIMARY KEY", "Name TEXT NOT NULL"],
		["Ar_Tag"] = ["TagId INTEGER PRIMARY KEY", "Name TEXT NOT NULL"],
		["Ar_ActivityTag"] = ["ActivityId INTEGER NOT NULL", "TagId INTEGER NOT NULL"],
		["Ar_Category"] = ["CategoryId INTEGER PRIMARY KEY", "Name TEXT NOT NULL"],
		["Ar_CategoryGroup"] = ["CategoryGroupId INTEGER PRIMARY KEY", "Name TEXT NOT NULL"],
	};

	private static void CreateTableWithout(SqliteConnection connection, string tableName, string columnToOmit)
	{
		if (TableColumnDefinitions.TryGetValue(tableName, out var allColumns))
		{
			var columns = allColumns.Where(c => !c.StartsWith(columnToOmit, StringComparison.OrdinalIgnoreCase)).ToArray();
			Execute(connection, $"CREATE TABLE {tableName} ({string.Join(", ", columns)})");
		}
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
			"ReportId INTEGER NOT NULL",
			"StartLocalTime TEXT NOT NULL",
			"EndLocalTime TEXT NOT NULL",
			"Name TEXT",
			"GroupId INTEGER",
			"Notes TEXT",
			"IsActive INTEGER DEFAULT 1",
			"CommonGroupId INTEGER",
			"StartUtcTime TEXT",
			"EndUtcTime TEXT",
		};
		columns.RemoveAll(c => c.StartsWith(columnToOmit, StringComparison.OrdinalIgnoreCase));
		Execute(connection, $"CREATE TABLE Ar_Activity ({string.Join(", ", columns)})");
	}

	private static void CreateGroupTableWithout(SqliteConnection connection, string columnToOmit)
	{
		var columns = new List<string>
		{
			"GroupId INTEGER NOT NULL",
			"ReportId INTEGER NOT NULL",
			"Name TEXT NOT NULL",
			"Color TEXT",
			"Key TEXT",
			"CommonId INTEGER",
			"GroupType TEXT",
		};
		columns.RemoveAll(c => c.StartsWith(columnToOmit, StringComparison.OrdinalIgnoreCase));
		var sql = $"CREATE TABLE Ar_Group ({string.Join(", ", columns)}, PRIMARY KEY (GroupId, ReportId))";
		Execute(connection, sql);
	}

	internal static void Execute(SqliteConnection connection, string sql)
	{
		using var command = connection.CreateCommand();
		command.CommandText = sql;
		command.ExecuteNonQuery();
	}
}
