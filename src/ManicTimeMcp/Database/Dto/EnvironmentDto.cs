namespace ManicTimeMcp.Database.Dto;

/// <summary>Environment/device info from Ar_Environment.</summary>
public sealed record EnvironmentDto
{
	/// <summary>Environment identifier.</summary>
	public required long EnvironmentId { get; init; }

	/// <summary>Device/computer name.</summary>
	public required string DeviceName { get; init; }
}
