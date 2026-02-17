using ManicTimeMcp.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ManicTimeMcp.Database;

/// <summary>DI registration for ManicTime database services.</summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers database connection factory, schema validator,
	/// and query repositories for ManicTime data access.
	/// </summary>
	public static IServiceCollection AddManicTimeDatabase(this IServiceCollection services)
	{
		services.AddSingleton<IDbConnectionFactory, SqliteConnectionFactory>();
		services.AddSingleton<ISchemaValidator, SchemaValidator>();
		services.AddSingleton<ITimelineRepository, TimelineRepository>();
		services.AddSingleton<IActivityRepository, ActivityRepository>();
		services.AddSingleton<IUsageRepository, UsageRepository>();
		services.AddSingleton<IEnvironmentRepository, EnvironmentRepository>();
		services.AddSingleton<ICorrelationRepository, CorrelationRepository>();
		services.AddSingleton(BuildCapabilityMatrix);
		return services;
	}

	/// <summary>
	/// Resolves data directory + runs schema validation at first DI resolution.
	/// Returns a fully-degraded matrix if the DB is inaccessible.
	/// HealthService.CheckSchema still calls Populate() to refresh on each health check.
	/// </summary>
	private static QueryCapabilityMatrix BuildCapabilityMatrix(IServiceProvider sp)
	{
		try
		{
			var resolver = sp.GetRequiredService<IDataDirectoryResolver>();
			var dir = resolver.Resolve();
			if (dir.Path is null)
			{
				return new QueryCapabilityMatrix([]);
			}

			var dbPath = Path.Combine(dir.Path, "ManicTimeReports.db");
			if (!File.Exists(dbPath))
			{
				return new QueryCapabilityMatrix([]);
			}

			var validator = sp.GetRequiredService<ISchemaValidator>();
			var result = validator.Validate(dbPath);
			return result.Capabilities ?? new QueryCapabilityMatrix([]);
		}
#pragma warning disable CA1031 // Startup must not crash — degrade gracefully
		catch (Exception ex)
#pragma warning restore CA1031
		{
			sp.GetService<ILogger<QueryCapabilityMatrix>>()?.StartupValidationFailed(ex);
			return new QueryCapabilityMatrix([]);
		}
	}
}
