using System.Diagnostics;
using System.Runtime.Versioning;
using Microsoft.Win32;

namespace ManicTimeMcp.Configuration;

/// <summary>Real OS-backed implementation of <see cref="IPlatformEnvironment"/>.</summary>
public sealed class PlatformEnvironment : IPlatformEnvironment
{
	internal const string ManicTimeRegistryKeyPath = @"SOFTWARE\FinKit\ManicTime";
	internal const string ManicTimeInstallDirValueName = "InstallDir";

	/// <inheritdoc />
	public bool FileExists(string path) => File.Exists(path);

	/// <inheritdoc />
	public long GetFileSize(string path) => new FileInfo(path).Length;

	/// <inheritdoc />
	public bool DirectoryExists(string path) => Directory.Exists(path);

	/// <inheritdoc />
	public bool DirectoryHasFiles(string path, string searchPattern, SearchOption searchOption)
	{
		try
		{
			return Directory.EnumerateFiles(path, searchPattern, searchOption).Any();
		}
#pragma warning disable CA1031 // Do not catch general exception types — directory enumeration may fail for various IO reasons
		catch (Exception)
#pragma warning restore CA1031
		{
			return false;
		}
	}

	/// <inheritdoc />
	public bool IsProcessRunning(string processName)
	{
		try
		{
			var processes = Process.GetProcessesByName(processName);
			var running = processes.Length > 0;
			foreach (var p in processes)
			{
				p.Dispose();
			}

			return running;
		}
#pragma warning disable CA1031 // Do not catch general exception types — process enumeration may fail on restricted environments
		catch (Exception)
#pragma warning restore CA1031
		{
			return false;
		}
	}

	/// <inheritdoc />
	public int? GetProcessId(string processName)
	{
		try
		{
			var processes = Process.GetProcessesByName(processName);
			int? pid = null;
			foreach (var p in processes)
			{
				pid ??= p.Id;
				p.Dispose();
			}

			return pid;
		}
#pragma warning disable CA1031 // Do not catch general exception types — process enumeration may fail on restricted environments
		catch (Exception)
#pragma warning restore CA1031
		{
			return null;
		}
	}

	/// <inheritdoc />
	public string? GetManicTimeInstallDir()
	{
		if (!OperatingSystem.IsWindows())
		{
			return null;
		}

		return TryReadInstallDir();
	}

	/// <inheritdoc />
	public string? GetFileProductVersion(string filePath)
	{
		try
		{
			if (!File.Exists(filePath))
			{
				return null;
			}

			var versionInfo = FileVersionInfo.GetVersionInfo(filePath);
			return versionInfo.ProductVersion;
		}
#pragma warning disable CA1031 // Do not catch general exception types — version info may fail for various IO reasons
		catch (Exception)
#pragma warning restore CA1031
		{
			return null;
		}
	}

	[SupportedOSPlatform("windows")]
	private static string? TryReadInstallDir()
	{
		try
		{
			using var key = Registry.LocalMachine.OpenSubKey(ManicTimeRegistryKeyPath);
			return key?.GetValue(ManicTimeInstallDirValueName) as string;
		}
#pragma warning disable CA1031 // Do not catch general exception types — registry may throw various platform exceptions
		catch (Exception)
#pragma warning restore CA1031
		{
			return null;
		}
	}
}
