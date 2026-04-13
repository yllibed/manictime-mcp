using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Mcp;

/// <summary>Resource payload for environment information.</summary>
public sealed record EnvironmentResource
{
	/// <summary>Environment entries from ManicTime.</summary>
	public required IReadOnlyList<EnvironmentDto> Environments { get; init; }
}
