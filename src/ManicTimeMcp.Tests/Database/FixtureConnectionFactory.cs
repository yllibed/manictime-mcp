using ManicTimeMcp.Database;
using Microsoft.Data.Sqlite;

namespace ManicTimeMcp.Tests.Database;

/// <summary>Connection factory for tests that returns connections to a fixture database.</summary>
internal sealed class FixtureConnectionFactory : IDbConnectionFactory
{
	private readonly string _databasePath;

	public FixtureConnectionFactory(string databasePath)
	{
		_databasePath = databasePath;
	}

	public SqliteConnection CreateConnection()
	{
		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = _databasePath,
			Mode = SqliteOpenMode.ReadOnly,
		}.ToString();

		var connection = new SqliteConnection(connectionString);
		connection.Open();
		return connection;
	}
}
