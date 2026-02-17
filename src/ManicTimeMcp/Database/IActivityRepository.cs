using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for ManicTime activity and group data.</summary>
public interface IActivityRepository
{
	/// <summary>
	/// Returns activities for the given timeline within the specified local time range,
	/// bounded by <see cref="QueryLimits.MaxActivities"/>.
	/// </summary>
	Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(
		long timelineId,
		string startLocalTime,
		string endLocalTime,
		int? limit = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns enriched activities with group details, common group names, and tags.
	/// Uses supplemental tables when available; falls back to core-only data otherwise.
	/// </summary>
	Task<IReadOnlyList<EnrichedActivityDto>> GetEnrichedActivitiesAsync(
		long timelineId,
		string startLocalTime,
		string endLocalTime,
		int? limit = null,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns groups for the given timeline, bounded by <see cref="QueryLimits.MaxGroups"/>.
	/// </summary>
	Task<IReadOnlyList<GroupDto>> GetGroupsAsync(long timelineId, CancellationToken cancellationToken = default);
}
