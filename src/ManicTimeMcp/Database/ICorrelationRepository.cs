using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for cross-timeline correlation queries.</summary>
public interface ICorrelationRepository
{
	/// <summary>
	/// Returns activities across all timelines overlapping the specified time window,
	/// joined with group and common group data when available.
	/// </summary>
	Task<IReadOnlyList<CorrelatedActivityDto>> GetCorrelatedActivitiesAsync(
		string startLocalTime,
		string endLocalTime,
		int? limit = null,
		CancellationToken cancellationToken = default);
}
