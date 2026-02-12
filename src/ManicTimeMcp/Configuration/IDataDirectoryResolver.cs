namespace ManicTimeMcp.Configuration;

/// <summary>Resolves the ManicTime data directory using a deterministic fallback chain.</summary>
public interface IDataDirectoryResolver
{
	/// <summary>Resolves and caches the data directory path (thread-safe, called once).</summary>
	DataDirectoryResult Resolve();
}
