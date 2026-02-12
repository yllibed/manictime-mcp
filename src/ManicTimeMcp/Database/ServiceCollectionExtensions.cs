using Microsoft.Extensions.DependencyInjection;

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
		return services;
	}
}
