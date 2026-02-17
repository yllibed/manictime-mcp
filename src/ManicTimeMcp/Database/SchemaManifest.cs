using System.Collections.Frozen;

namespace ManicTimeMcp.Database;

/// <summary>
/// Defines the expected database schema for ManicTimeReports.db.
/// The manifest is the single source of truth for required tables and columns,
/// organized into tiers: Core (fatal if missing), Supplemental (warning),
/// and Informational (cosmetic loss).
/// </summary>
public static class SchemaManifest
{
	/// <summary>Expected table definitions keyed by table name.</summary>
	public static FrozenDictionary<string, TableDefinition> Tables { get; } = CreateManifest();

	private static FrozenDictionary<string, TableDefinition> CreateManifest()
	{
		var tables = new Dictionary<string, TableDefinition>(StringComparer.OrdinalIgnoreCase);

		AddCoreTables(tables);
		AddSupplementalTables(tables);
		AddInformationalTables(tables);

		return tables.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
	}

	private static void AddCoreTables(Dictionary<string, TableDefinition> tables)
	{
		tables["Ar_Timeline"] = new(
			"Ar_Timeline",
			["ReportId", "SchemaName", "BaseSchemaName"],
			TableTier.Core);

		tables["Ar_Activity"] = new(
			"Ar_Activity",
			[
				"ActivityId", "ReportId", "StartLocalTime", "EndLocalTime", "Name", "GroupId",
				"Notes", "IsActive", "CommonGroupId", "StartUtcTime", "EndUtcTime",
			],
			TableTier.Core);

		tables["Ar_Group"] = new(
			"Ar_Group",
			["GroupId", "ReportId", "Name", "Color", "Key", "CommonId", "GroupType"],
			TableTier.Core);
	}

	private static void AddSupplementalTables(Dictionary<string, TableDefinition> tables)
	{
		tables["Ar_CommonGroup"] = new(
			"Ar_CommonGroup",
			["CommonGroupId", "Name", "Color", "Key"],
			TableTier.Supplemental);

		tables["Ar_ApplicationByDay"] = new("Ar_ApplicationByDay", ["Day", "CommonGroupId", "TotalSeconds"], TableTier.Supplemental);
		tables["Ar_WebSiteByDay"] = new("Ar_WebSiteByDay", ["Day", "CommonGroupId", "TotalSeconds"], TableTier.Supplemental);
		tables["Ar_DocumentByDay"] = new("Ar_DocumentByDay", ["Day", "CommonGroupId", "TotalSeconds"], TableTier.Supplemental);
		tables["Ar_ApplicationByYear"] = new("Ar_ApplicationByYear", ["Day", "CommonGroupId", "TotalSeconds"], TableTier.Supplemental);
		tables["Ar_WebSiteByYear"] = new("Ar_WebSiteByYear", ["Day", "CommonGroupId", "TotalSeconds"], TableTier.Supplemental);
		tables["Ar_DocumentByYear"] = new("Ar_DocumentByYear", ["Day", "CommonGroupId", "TotalSeconds"], TableTier.Supplemental);

		tables["Ar_ActivityByHour"] = new(
			"Ar_ActivityByHour",
			["Day", "Hour", "CommonGroupId", "TotalSeconds"],
			TableTier.Supplemental);

		tables["Ar_TimelineSummary"] = new("Ar_TimelineSummary", ["ReportId", "StartLocalTime", "EndLocalTime"], TableTier.Supplemental);
		tables["Ar_Environment"] = new("Ar_Environment", ["EnvironmentId", "DeviceName"], TableTier.Supplemental);
		tables["Ar_Folder"] = new("Ar_Folder", ["FolderId", "Name"], TableTier.Supplemental);
		tables["Ar_Tag"] = new("Ar_Tag", ["TagId", "Name"], TableTier.Supplemental);
		tables["Ar_ActivityTag"] = new("Ar_ActivityTag", ["ActivityId", "TagId"], TableTier.Supplemental);
	}

	private static void AddInformationalTables(Dictionary<string, TableDefinition> tables)
	{
		tables["Ar_Category"] = new("Ar_Category", ["CategoryId", "Name"], TableTier.Informational);
		tables["Ar_CategoryGroup"] = new("Ar_CategoryGroup", ["CategoryGroupId", "Name"], TableTier.Informational);
	}
}
