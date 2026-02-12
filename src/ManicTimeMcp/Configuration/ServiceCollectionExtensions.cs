using Microsoft.Extensions.DependencyInjection;

namespace ManicTimeMcp.Configuration;

/// <summary>DI registration for ManicTime configuration and health services.</summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Registers ManicTime data directory resolution, platform environment,
	/// and health diagnostic services.
	/// </summary>
	public static IServiceCollection AddManicTimeConfiguration(this IServiceCollection services)
	{
		services.AddSingleton<IPlatformEnvironment, PlatformEnvironment>();
		services.AddSingleton<IDataDirectoryResolver, DataDirectoryResolver>();
		services.AddSingleton<IHealthService, HealthService>();
		return services;
	}
}
