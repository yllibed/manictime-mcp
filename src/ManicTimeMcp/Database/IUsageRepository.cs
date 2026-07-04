using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for pre-aggregated usage data.</summary>
public interface IUsageRepository
{
	/// <summary>Returns hourly application usage for a date range.</summary>
	Task<IReadOnlyList<HourlyUsageDto>> GetHourlyAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default);

	/// <summary>Returns hourly website usage for a date range.</summary>
	Task<IReadOnlyList<HourlyUsageDto>> GetHourlyWebUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default);

	/// <summary>Returns daily application usage for a date range.</summary>
	Task<IReadOnlyList<DailyUsageDto>> GetDailyAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default);

	/// <summary>Returns total application usage seconds for a date range; implementations should avoid display row caps.</summary>
	async Task<double> GetTotalAppUsageSecondsAsync(
		string startDay, string endDay, CancellationToken cancellationToken = default)
	{
		var usage = await GetDailyAppUsageAsync(
			startDay,
			endDay,
			QueryLimits.MaxDailyUsageRows,
			cancellationToken).ConfigureAwait(false);
		return usage.Sum(row => row.TotalSeconds);
	}

	/// <summary>Returns daily website usage for a date range.</summary>
	Task<IReadOnlyList<DailyUsageDto>> GetDailyWebUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default);

	/// <summary>Returns daily document usage for a date range.</summary>
	Task<IReadOnlyList<DailyUsageDto>> GetDailyDocUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default);

	/// <summary>Returns daily tag usage for a date range.</summary>
	Task<IReadOnlyList<DailyUsageDto>> GetDailyTagUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default);

	/// <summary>Returns day-of-week aggregated application usage for pattern analysis.</summary>
	Task<IReadOnlyList<DayOfWeekUsageDto>> GetDayOfWeekAppUsageAsync(
		string startDay, string endDay, int? limit = null, CancellationToken cancellationToken = default);

	/// <summary>Returns timeline summaries showing data coverage ranges.</summary>
	Task<IReadOnlyList<TimelineSummaryDto>> GetTimelineSummariesAsync(CancellationToken cancellationToken = default);
}
