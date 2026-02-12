namespace ManicTimeMcp.Configuration;

/// <summary>Abstraction for OS-level operations used by health checks (enables testability).</summary>
public interface IPlatformEnvironment
{
	/// <summary>Checks whether a file exists at the specified path.</summary>
	bool FileExists(string path);

	/// <summary>Gets the size of a file in bytes.</summary>
	long GetFileSize(string path);

	/// <summary>Checks whether a directory exists at the specified path.</summary>
	bool DirectoryExists(string path);

	/// <summary>Checks whether a directory contains any files matching the search pattern.</summary>
	bool DirectoryHasFiles(string path, string searchPattern, SearchOption searchOption);

	/// <summary>Checks whether a process with the given name is currently running.</summary>
	bool IsProcessRunning(string processName);
}
