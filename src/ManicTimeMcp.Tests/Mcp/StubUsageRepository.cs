using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubUsageRepository(
	IReadOnlyList<DailyUsageDto>? dailyApp = null,
	IReadOnlyList<DailyUsageDto>? dailyWeb = null,
	IReadOnlyList<DailyUsageDto>? dailyDoc = null,
	IReadOnlyList<HourlyUsageDto>? hourlyApp = null,
	IReadOnlyList<HourlyUsageDto>? hourlyWeb = null,
	IReadOnlyList<TimelineSummaryDto>? summaries = null) : IUsageRepository
{
	public Task<IReadOnlyList<HourlyUsageDto>> GetHourlyAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<HourlyUsageDto>>(hourlyApp ?? []);

	public Task<IReadOnlyList<HourlyUsageDto>> GetHourlyWebUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<HourlyUsageDto>>(hourlyWeb ?? []);

	public Task<IReadOnlyList<DailyUsageDto>> GetDailyAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<DailyUsageDto>>(dailyApp ?? []);

	public Task<IReadOnlyList<DailyUsageDto>> GetDailyWebUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<DailyUsageDto>>(dailyWeb ?? []);

	public Task<IReadOnlyList<DailyUsageDto>> GetDailyDocUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<DailyUsageDto>>(dailyDoc ?? []);

	public Task<IReadOnlyList<DayOfWeekUsageDto>> GetDayOfWeekAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<DayOfWeekUsageDto>>([]);

	public Task<IReadOnlyList<TimelineSummaryDto>> GetTimelineSummariesAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<TimelineSummaryDto>>(summaries ?? []);
}
