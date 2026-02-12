using ManicTimeMcp.Configuration;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>
/// Creates read-only SQLite connections to ManicTimeReports.db
/// using the resolved data directory path.
/// </summary>
public sealed class SqliteConnectionFactory : IDbConnectionFactory
{
	private readonly IDataDirectoryResolver _resolver;
	private readonly ILogger<SqliteConnectionFactory> _logger;

	/// <summary>Creates a new connection factory.</summary>
	public SqliteConnectionFactory(IDataDirectoryResolver resolver, ILogger<SqliteConnectionFactory> logger)
	{
		_resolver = resolver;
		_logger = logger;
	}

	/// <inheritdoc />
	public SqliteConnection CreateConnection()
	{
		var result = _resolver.Resolve();

		if (result.Path is null)
		{
			throw new InvalidOperationException("Cannot create database connection: data directory is not resolved.");
		}

		var dbPath = Path.Combine(result.Path, HealthService.DatabaseFileName);

		if (!File.Exists(dbPath))
		{
			throw new InvalidOperationException($"Cannot create database connection: database file not found at '{dbPath}'.");
		}

		var connectionString = new SqliteConnectionStringBuilder
		{
			DataSource = dbPath,
			Mode = SqliteOpenMode.ReadOnly,
		}.ToString();

		_logger.DatabaseConnectionCreated(dbPath);

		var connection = new SqliteConnection(connectionString);
		connection.Open();
		return connection;
	}
}
