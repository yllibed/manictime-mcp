namespace ManicTimeMcp.Screenshots;

/// <summary>Strategy for selecting which screenshots to include in results.</summary>
public enum SamplingStrategy
{
	/// <summary>Select screenshots at regular time intervals.</summary>
	Interval,

	/// <summary>Select screenshots near activity transitions (application switches).</summary>
	ActivityTransition,
}
