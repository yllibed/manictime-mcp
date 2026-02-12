using System.Collections.Frozen;

namespace ManicTimeMcp.Database;

/// <summary>
/// Defines the expected database schema for ManicTimeReports.db.
/// The manifest is the single source of truth for required tables and columns.
/// </summary>
public static class SchemaManifest
{
	/// <summary>Expected table definitions keyed by table name.</summary>
	public static FrozenDictionary<string, TableDefinition> Tables { get; } = CreateManifest();

	private static FrozenDictionary<string, TableDefinition> CreateManifest()
	{
		var tables = new Dictionary<string, TableDefinition>(StringComparer.OrdinalIgnoreCase)
		{
			["Ar_Timeline"] = new(
				"Ar_Timeline",
				["ReportId", "SchemaName", "BaseSchemaName"]),

			["Ar_Activity"] = new(
				"Ar_Activity",
				["ActivityId", "TimelineId", "StartLocalTime", "EndLocalTime", "DisplayName", "GroupId"]),

			["Ar_Group"] = new(
				"Ar_Group",
				["GroupId", "TimelineId", "DisplayName", "ParentGroupId"]),
		};

		return tables.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
	}
}
