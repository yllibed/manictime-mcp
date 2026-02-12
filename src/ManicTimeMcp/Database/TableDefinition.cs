using System.Collections.Frozen;

namespace ManicTimeMcp.Database;

/// <summary>Describes a required database table and its expected columns.</summary>
/// <param name="TableName">Case-insensitive table name.</param>
/// <param name="RequiredColumns">Column names that must be present.</param>
public sealed record TableDefinition(string TableName, FrozenSet<string> RequiredColumns)
{
	/// <summary>Creates a table definition from a table name and column names.</summary>
	public TableDefinition(string tableName, IEnumerable<string> requiredColumns)
		: this(tableName, requiredColumns.ToFrozenSet(StringComparer.OrdinalIgnoreCase))
	{
	}
}
