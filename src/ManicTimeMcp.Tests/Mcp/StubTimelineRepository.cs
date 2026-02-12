using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubTimelineRepository(IReadOnlyList<TimelineDto>? timelines = null) : ITimelineRepository
{
	private readonly IReadOnlyList<TimelineDto> _timelines = timelines ?? [];

	public Task<IReadOnlyList<TimelineDto>> GetTimelinesAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult(_timelines);
}
