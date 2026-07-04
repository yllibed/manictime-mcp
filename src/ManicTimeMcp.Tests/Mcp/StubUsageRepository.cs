using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubUsageRepository(
	IReadOnlyList<DailyUsageDto>? dailyApp = null,
	double? totalAppSeconds = null,
	IReadOnlyList<DailyUsageDto>? dailyWeb = null,
	IReadOnlyList<DailyUsageDto>? dailyDoc = null,
	IReadOnlyList<DailyUsageDto>? dailyTag = null,
	IReadOnlyList<HourlyUsageDto>? hourlyApp = null,
	IReadOnlyList<HourlyUsageDto>? hourlyWeb = null,
	IReadOnlyList<TimelineSummaryDto>? summaries = null) : IUsageRepository
{
	public int? LastDailyAppLimit { get; private set; }

	public bool TotalAppUsageRequested { get; private set; }

	public int? LastDailyWebLimit { get; private set; }

	public int? LastDailyDocLimit { get; private set; }

	public int? LastDailyTagLimit { get; private set; }

	public Task<IReadOnlyList<HourlyUsageDto>> GetHourlyAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<HourlyUsageDto>>(hourlyApp ?? []);

	public Task<IReadOnlyList<HourlyUsageDto>> GetHourlyWebUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<HourlyUsageDto>>(hourlyWeb ?? []);

	public Task<IReadOnlyList<DailyUsageDto>> GetDailyAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default)
	{
		LastDailyAppLimit = limit;
		return Task.FromResult<IReadOnlyList<DailyUsageDto>>(dailyApp ?? []);
	}

	public Task<double> GetTotalAppUsageSecondsAsync(
		string startDay, string endDay, CancellationToken cancellationToken = default)
	{
		TotalAppUsageRequested = true;
		return Task.FromResult(totalAppSeconds ?? (dailyApp?.Sum(usage => usage.TotalSeconds) ?? 0));
	}

	public Task<IReadOnlyList<DailyUsageDto>> GetDailyWebUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default)
	{
		LastDailyWebLimit = limit;
		return Task.FromResult<IReadOnlyList<DailyUsageDto>>(dailyWeb ?? []);
	}

	public Task<IReadOnlyList<DailyUsageDto>> GetDailyDocUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default)
	{
		LastDailyDocLimit = limit;
		return Task.FromResult<IReadOnlyList<DailyUsageDto>>(dailyDoc ?? []);
	}

	public Task<IReadOnlyList<DailyUsageDto>> GetDailyTagUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default)
	{
		LastDailyTagLimit = limit;
		return Task.FromResult<IReadOnlyList<DailyUsageDto>>(dailyTag ?? []);
	}

	public Task<IReadOnlyList<DayOfWeekUsageDto>> GetDayOfWeekAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<DayOfWeekUsageDto>>([]);

	public Task<IReadOnlyList<TimelineSummaryDto>> GetTimelineSummariesAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<TimelineSummaryDto>>(summaries ?? []);
}
