using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>
/// Provides retry logic for transient <c>SQLITE_BUSY</c> errors.
/// ManicTime may hold brief locks during writes; read-only queries
/// should retry a bounded number of times with exponential backoff.
/// </summary>
internal static class SqliteRetryHelper
{
	private const int MaxAttempts = 3;
	private static readonly TimeSpan[] Delays = [TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(200)];

	/// <summary>Executes an async action with retry logic for SQLITE_BUSY.</summary>
	internal static async Task<T> ExecuteWithRetryAsync<T>(ILogger logger, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken)
	{
		for (var attempt = 1; attempt <= MaxAttempts; attempt++)
		{
			try
			{
				return await action(cancellationToken).ConfigureAwait(false);
			}
			catch (SqliteException ex) when (ex.SqliteErrorCode == 5 && attempt < MaxAttempts) // SQLITE_BUSY = 5
			{
				logger.SqliteBusyRetry(attempt, MaxAttempts);
				await Task.Delay(Delays[attempt - 1], cancellationToken).ConfigureAwait(false);
			}
		}

		throw new InvalidOperationException("Retry loop completed without returning or throwing.");
	}
}
