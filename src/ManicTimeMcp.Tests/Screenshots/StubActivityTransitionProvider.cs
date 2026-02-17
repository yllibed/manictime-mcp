using ManicTimeMcp.Screenshots;

namespace ManicTimeMcp.Tests.Screenshots;

/// <summary>Configurable stub for activity transition detection in tests.</summary>
internal sealed class StubActivityTransitionProvider(IReadOnlyList<ActivityTransition>? transitions = null)
	: IActivityTransitionProvider
{
	private readonly IReadOnlyList<ActivityTransition> _transitions = transitions ?? [];

	public Task<IReadOnlyList<ActivityTransition>> GetTransitionsAsync(
		DateTime startLocalTime, DateTime endLocalTime, CancellationToken cancellationToken = default)
	{
		var filtered = _transitions
			.Where(t => t.Timestamp >= startLocalTime && t.Timestamp < endLocalTime)
			.ToList();
		return Task.FromResult<IReadOnlyList<ActivityTransition>>(filtered);
	}
}
