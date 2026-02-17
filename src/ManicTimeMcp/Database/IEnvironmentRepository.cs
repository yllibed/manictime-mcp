using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Database;

/// <summary>Read-only repository for environment/device info.</summary>
public interface IEnvironmentRepository
{
	/// <summary>Returns all known environment entries.</summary>
	Task<IReadOnlyList<EnvironmentDto>> GetEnvironmentsAsync(CancellationToken cancellationToken = default);
}
