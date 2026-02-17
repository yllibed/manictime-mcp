using System.Collections.Frozen;

namespace ManicTimeMcp.Database;

/// <summary>
/// Records which supplemental tables are fully validated in the database,
/// enabling repositories to select optimal query paths.
/// Starts fully-degraded; call <see cref="Populate"/> after schema validation.
/// </summary>
public sealed class QueryCapabilityMatrix
{
	/// <summary>Per-table presence map (thread-safe via volatile swap).</summary>
	public FrozenDictionary<string, bool> TablePresence
	{
		get => _tablePresence;
		private set => _tablePresence = value;
	}

	private volatile FrozenDictionary<string, bool> _tablePresence;

	/// <summary>Creates a capability matrix from a set of validated table names.</summary>
	public QueryCapabilityMatrix(IEnumerable<string> validatedTables)
	{
		_tablePresence = BuildPresenceMap(validatedTables);
	}

	/// <summary>
	/// Replaces capabilities after schema validation completes.
	/// Only tables that exist AND pass column validation are included.
	/// Thread-safe: repositories see the old or new map, never a torn read.
	/// </summary>
	public void Populate(IEnumerable<string> validatedTables)
	{
		_tablePresence = BuildPresenceMap(validatedTables);
	}

	private static FrozenDictionary<string, bool> BuildPresenceMap(IEnumerable<string> validatedTables)
	{
		var present = new HashSet<string>(validatedTables, StringComparer.OrdinalIgnoreCase);
		var map = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

		foreach (var table in SchemaManifest.Tables.Values)
		{
			if (table.Tier != TableTier.Core)
			{
				map[table.TableName] = present.Contains(table.TableName);
			}
		}

		return map.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
	}

	/// <summary>Whether pre-aggregated application usage tables are available.</summary>
	public bool HasPreAggregatedAppUsage => Has("Ar_ApplicationByDay") && Has("Ar_CommonGroup");

	/// <summary>Whether pre-aggregated website usage tables are available.</summary>
	public bool HasPreAggregatedWebUsage => Has("Ar_WebSiteByDay") && Has("Ar_CommonGroup");

	/// <summary>Whether pre-aggregated document usage tables are available.</summary>
	public bool HasPreAggregatedDocUsage => Has("Ar_DocumentByDay") && Has("Ar_CommonGroup");

	/// <summary>Whether hourly activity data is available.</summary>
	public bool HasHourlyUsage => Has("Ar_ActivityByHour") && Has("Ar_CommonGroup");

	/// <summary>Whether yearly aggregation tables are available.</summary>
	public bool HasYearlyUsage => Has("Ar_ApplicationByYear") && Has("Ar_CommonGroup");

	/// <summary>Whether common group lookups are available.</summary>
	public bool HasCommonGroup => Has("Ar_CommonGroup");

	/// <summary>Whether tag data is available.</summary>
	public bool HasTags => Has("Ar_Tag") && Has("Ar_ActivityTag");

	/// <summary>Whether timeline summary data is available.</summary>
	public bool HasTimelineSummary => Has("Ar_TimelineSummary");

	/// <summary>Whether environment/device info is available.</summary>
	public bool HasEnvironment => Has("Ar_Environment");

	/// <summary>Whether folder data is available.</summary>
	public bool HasFolders => Has("Ar_Folder");

	/// <summary>Whether category data is available.</summary>
	public bool HasCategories => Has("Ar_Category") && Has("Ar_CategoryGroup");

	/// <summary>Returns all capability names that are degraded (unavailable).</summary>
	public IReadOnlyList<string> GetDegradedCapabilities()
	{
		var degraded = new List<string>();
		if (!HasPreAggregatedAppUsage)
		{
			degraded.Add("PreAggregatedAppUsage");
		}

		if (!HasPreAggregatedWebUsage)
		{
			degraded.Add("PreAggregatedWebUsage");
		}

		if (!HasPreAggregatedDocUsage)
		{
			degraded.Add("PreAggregatedDocUsage");
		}

		if (!HasHourlyUsage)
		{
			degraded.Add("HourlyUsage");
		}

		if (!HasCommonGroup)
		{
			degraded.Add("CommonGroup");
		}

		if (!HasTags)
		{
			degraded.Add("Tags");
		}

		if (!HasTimelineSummary)
		{
			degraded.Add("TimelineSummary");
		}

		if (!HasEnvironment)
		{
			degraded.Add("Environment");
		}

		return degraded;
	}

	private bool Has(string tableName) =>
		TablePresence.TryGetValue(tableName, out var present) && present;
}
