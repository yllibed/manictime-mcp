namespace ManicTimeMcp.Database.Dto;

/// <summary>Hourly usage aggregation from Ar_ActivityByHour + Ar_CommonGroup.</summary>
public sealed record HourlyUsageDto
{
	/// <summary>Day (yyyy-MM-dd).</summary>
	public required string Day { get; init; }

	/// <summary>Hour of day (0-23).</summary>
	public required int Hour { get; init; }

	/// <summary>Application/website/document name.</summary>
	public required string Name { get; init; }

	/// <summary>Group color.</summary>
	public string? Color { get; init; }

	/// <summary>Group key.</summary>
	public string? Key { get; init; }

	/// <summary>Total usage in seconds.</summary>
	public required double TotalSeconds { get; init; }
}
