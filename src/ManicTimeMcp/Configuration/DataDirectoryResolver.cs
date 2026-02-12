using System.Runtime.Versioning;
using ManicTimeMcp.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace ManicTimeMcp.Configuration;

/// <summary>
/// Platform-aware data directory resolver using fallback chain:
/// MANICTIME_DATA_DIR env var, Windows registry, %LOCALAPPDATA% default, unresolved.
/// </summary>
public sealed class DataDirectoryResolver : IDataDirectoryResolver
{
	internal const string EnvironmentVariableName = "MANICTIME_DATA_DIR";
	internal const string RegistryKeyPath = @"SOFTWARE\Finkit\ManicTime";
	internal const string RegistryValueName = "DataDirectory";
	internal const string LocalAppDataSubPath = @"Finkit\ManicTime";

	private readonly Lazy<DataDirectoryResult> _cached;
	private readonly ILogger<DataDirectoryResolver> _logger;

	/// <summary>Creates a new resolver with structured logging.</summary>
	public DataDirectoryResolver(ILogger<DataDirectoryResolver> logger)
	{
		_logger = logger;
		_cached = new Lazy<DataDirectoryResult>(ResolveCore);
	}

	/// <inheritdoc />
	public DataDirectoryResult Resolve() => _cached.Value;

	/// <summary>
	/// Pure resolution logic without OS dependencies — fully testable.
	/// Pass pre-fetched values; null for unavailable candidates.
	/// </summary>
	internal static DataDirectoryResult Resolve(
		string? environmentVariable,
		bool isWindows,
		string? registryValue,
		string? localAppDataCandidate)
	{
		if (!string.IsNullOrWhiteSpace(environmentVariable))
		{
			return new DataDirectoryResult
			{
				Path = environmentVariable.Trim(),
				Source = DataDirectorySource.EnvironmentVariable,
			};
		}

		if (isWindows && !string.IsNullOrWhiteSpace(registryValue))
		{
			return new DataDirectoryResult
			{
				Path = registryValue.Trim(),
				Source = DataDirectorySource.Registry,
			};
		}

		if (isWindows && localAppDataCandidate is not null)
		{
			return new DataDirectoryResult
			{
				Path = localAppDataCandidate,
				Source = DataDirectorySource.LocalAppData,
			};
		}

		return new DataDirectoryResult
		{
			Path = null,
			Source = DataDirectorySource.Unresolved,
		};
	}

	private DataDirectoryResult ResolveCore()
	{
		var envVar = Environment.GetEnvironmentVariable(EnvironmentVariableName);

		string? registryValue = null;
		if (OperatingSystem.IsWindows())
		{
			registryValue = TryReadRegistryValue();
		}

		var localAppDataCandidate = OperatingSystem.IsWindows()
			? GetLocalAppDataCandidate()
			: null;

		if (localAppDataCandidate is not null && !Directory.Exists(localAppDataCandidate))
		{
			localAppDataCandidate = null;
		}

		var result = Resolve(envVar, OperatingSystem.IsWindows(), registryValue, localAppDataCandidate);

		if (result.Path is not null)
		{
			_logger.DataDirectoryResolved(result.Source, result.Path);
		}
		else
		{
			_logger.DataDirectoryUnresolved();
		}

		return result;
	}

	[SupportedOSPlatform("windows")]
	private static string? TryReadRegistryValue()
	{
		try
		{
			using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath);
			return key?.GetValue(RegistryValueName) as string;
		}
#pragma warning disable CA1031 // Do not catch general exception types — registry may throw various platform exceptions
		catch (Exception)
#pragma warning restore CA1031
		{
			return null;
		}
	}

	private static string? GetLocalAppDataCandidate()
	{
		var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
		return string.IsNullOrEmpty(localAppData)
			? null
			: System.IO.Path.Combine(localAppData, LocalAppDataSubPath);
	}
}
