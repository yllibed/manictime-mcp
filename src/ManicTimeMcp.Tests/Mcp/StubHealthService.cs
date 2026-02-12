using ManicTimeMcp.Configuration;
using ManicTimeMcp.Models;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubHealthService : IHealthService
{
	public HealthReport GetHealthReport() => new()
	{
		Status = HealthStatus.Healthy,
		DataDirectory = @"C:\TestData",
		DirectorySource = DataDirectorySource.LocalAppData,
		DatabaseExists = true,
		DatabaseSizeBytes = 1024,
		SchemaStatus = SchemaValidationStatus.Valid,
		ManicTimeProcessRunning = false,
		ManicTimeProcessId = null,
		ManicTimeVersion = null,
		Screenshots = new ScreenshotAvailability
		{
			Status = ScreenshotAvailabilityStatus.Available,
			Reason = ScreenshotUnavailableReason.None,
		},
		Issues = [],
	};
}
