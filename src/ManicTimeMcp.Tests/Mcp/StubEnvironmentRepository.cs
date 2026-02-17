using ManicTimeMcp.Database;
using ManicTimeMcp.Database.Dto;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubEnvironmentRepository(IReadOnlyList<EnvironmentDto>? environments = null) : IEnvironmentRepository
{
	public Task<IReadOnlyList<EnvironmentDto>> GetEnvironmentsAsync(CancellationToken cancellationToken = default) =>
		Task.FromResult<IReadOnlyList<EnvironmentDto>>(environments ?? []);
}
