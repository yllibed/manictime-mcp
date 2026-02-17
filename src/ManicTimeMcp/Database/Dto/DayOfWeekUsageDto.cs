namespace ManicTimeMcp.Database.Dto;

/// <summary>Read model for day-of-week usage pattern aggregation.</summary>
public sealed record DayOfWeekUsageDto
{
	/// <summary>Application or group display name.</summary>
	public required string Name { get; init; }

	/// <summary>Day of week (0 = Sunday, 6 = Saturday).</summary>
	public required int DayOfWeek { get; init; }

	/// <summary>Total seconds of usage for this name+day combination.</summary>
	public required double TotalSeconds { get; init; }
}
