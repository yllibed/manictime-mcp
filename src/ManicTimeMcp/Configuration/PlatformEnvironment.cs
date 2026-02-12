using System.Diagnostics;

namespace ManicTimeMcp.Configuration;

/// <summary>Real OS-backed implementation of <see cref="IPlatformEnvironment"/>.</summary>
public sealed class PlatformEnvironment : IPlatformEnvironment
{
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
}
