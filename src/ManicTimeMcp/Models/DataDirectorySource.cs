namespace ManicTimeMcp.Models;

/// <summary>How the ManicTime data directory was resolved.</summary>
public enum DataDirectorySource
{
	/// <summary>Set via the MANICTIME_DATA_DIR environment variable.</summary>
	EnvironmentVariable,

	/// <summary>Read from the Windows registry (HKCU).</summary>
	Registry,

	/// <summary>Inferred from the standard %LOCALAPPDATA% path.</summary>
	LocalAppData,

	/// <summary>Could not be resolved through any known method.</summary>
	Unresolved,
}
