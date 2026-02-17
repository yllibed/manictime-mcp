using System.Collections.Frozen;

namespace ManicTimeMcp.Database;

/// <summary>Describes a database table and its expected columns.</summary>
/// <param name="TableName">Case-insensitive table name.</param>
/// <param name="RequiredColumns">Column names that must be present.</param>
/// <param name="Tier">Criticality tier controlling validation severity.</param>
public sealed record TableDefinition(string TableName, FrozenSet<string> RequiredColumns, TableTier Tier)
{
	/// <summary>Creates a table definition from a table name, column names, and tier.</summary>
	public TableDefinition(string tableName, IEnumerable<string> requiredColumns, TableTier tier = TableTier.Core)
		: this(tableName, requiredColumns.ToFrozenSet(StringComparer.OrdinalIgnoreCase), tier)
	{
	}
}
