using Microsoft.Data.Sqlite;

namespace ManicTimeMcp.Database;

/// <summary>Factory for creating read-only SQLite connections to ManicTimeReports.db.</summary>
public interface IDbConnectionFactory
{
	/// <summary>Creates a new open read-only connection to the ManicTime database.</summary>
	/// <exception cref="InvalidOperationException">The data directory is not resolved or the database does not exist.</exception>
	SqliteConnection CreateConnection();
}
