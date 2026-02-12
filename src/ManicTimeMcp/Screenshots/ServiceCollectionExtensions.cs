using Microsoft.Extensions.DependencyInjection;

namespace ManicTimeMcp.Screenshots;

/// <summary>DI registration for ManicTime screenshot services.</summary>
public static class ServiceCollectionExtensions
{
	/// <summary>Registers screenshot selection and reading services.</summary>
	public static IServiceCollection AddManicTimeScreenshots(this IServiceCollection services)
	{
		services.AddSingleton<IScreenshotService, ScreenshotService>();
		return services;
	}
}
