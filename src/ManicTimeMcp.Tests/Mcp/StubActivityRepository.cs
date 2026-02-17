using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubActivityRepository(
	IReadOnlyList<ActivityDto>? activities = null,
	IReadOnlyList<EnrichedActivityDto>? enrichedActivities = null) : IActivityRepository
{
	private readonly IReadOnlyList<ActivityDto> _activities = activities ?? [];
	private readonly IReadOnlyList<EnrichedActivityDto>? _enrichedActivities = enrichedActivities;

	public Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(
		long timelineId, string startLocalTime, string endLocalTime, int? limit = null,
		CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<ActivityDto>>(
			_activities.Where(a => a.ReportId == timelineId).ToList());

	public Task<IReadOnlyList<EnrichedActivityDto>> GetEnrichedActivitiesAsync(
		long timelineId, string startLocalTime, string endLocalTime, int? limit = null,
		CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<EnrichedActivityDto>>(
			_enrichedActivities?.Where(a => a.ReportId == timelineId).ToList()
			?? _activities
				.Where(a => a.ReportId == timelineId)
				.Select(a => new EnrichedActivityDto
				{
					ActivityId = a.ActivityId,
					ReportId = a.ReportId,
					StartLocalTime = a.StartLocalTime,
					EndLocalTime = a.EndLocalTime,
					Name = a.Name,
					GroupId = a.GroupId,
				})
				.ToList());

	public Task<IReadOnlyList<GroupDto>> GetGroupsAsync(long timelineId, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<GroupDto>>([]);
}
