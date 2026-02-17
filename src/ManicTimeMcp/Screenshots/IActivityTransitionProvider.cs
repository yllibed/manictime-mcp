namespace ManicTimeMcp.Screenshots;

/// <summary>Provides activity transitions within a time window for screenshot sampling.</summary>
public interface IActivityTransitionProvider
{
	/// <summary>
	/// Returns activity transitions (application switches) within the specified time range.
	/// Used by the activity-transition sampling strategy to select screenshots near transitions.
	/// </summary>
	Task<IReadOnlyList<ActivityTransition>> GetTransitionsAsync(
		DateTime startLocalTime,
		DateTime endLocalTime,
		CancellationToken cancellationToken = default);
}
