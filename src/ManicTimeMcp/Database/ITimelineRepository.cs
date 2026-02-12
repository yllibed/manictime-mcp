using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for ManicTime timeline data.</summary>
public interface ITimelineRepository
{
	/// <summary>Returns all timelines ordered by ReportId, bounded by <see cref="QueryLimits.MaxTimelines"/>.</summary>
	Task<IReadOnlyList<TimelineDto>> GetTimelinesAsync(CancellationToken cancellationToken = default);
}
