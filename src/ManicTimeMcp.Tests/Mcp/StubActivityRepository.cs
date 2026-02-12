using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubActivityRepository(IReadOnlyList<ActivityDto>? activities = null) : IActivityRepository
{
	private readonly IReadOnlyList<ActivityDto> _activities = activities ?? [];

	public Task<IReadOnlyList<ActivityDto>> GetActivitiesAsync(
		long timelineId, string startLocalTime, string endLocalTime, int? limit = null,
		CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<ActivityDto>>(
			_activities.Where(a => a.TimelineId == timelineId).ToList());

	public Task<IReadOnlyList<GroupDto>> GetGroupsAsync(long timelineId, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<GroupDto>>([]);
}
