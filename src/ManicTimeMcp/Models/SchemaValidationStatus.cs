namespace ManicTimeMcp.Models;

/// <summary>Result of database schema validation.</summary>
public enum SchemaValidationStatus
{
	/// <summary>Schema matches expected structure.</summary>
	Valid,

	/// <summary>Schema does not match expected structure.</summary>
	Invalid,

	/// <summary>Schema validation was not performed (database missing or deferred to WS-04).</summary>
	NotChecked,
}
