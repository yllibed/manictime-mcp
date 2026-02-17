namespace ManicTimeMcp.Database;

/// <summary>Classification of database tables by criticality for graceful degradation.</summary>
public enum TableTier
{
	/// <summary>Essential tables required for basic operation. Missing = fatal.</summary>
	Core,

	/// <summary>Tables that enable richer queries. Missing = warning with degraded results.</summary>
	Supplemental,

	/// <summary>Reference tables for display/classification. Missing = cosmetic loss only.</summary>
	Informational,
}
