using ManicTimeMcp.Database;

namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Detects activity transitions by querying the Applications timeline
/// for sequential activities with different group names.
/// </summary>
public sealed class DatabaseActivityTransitionProvider : IActivityTransitionProvider
{
	private readonly IActivityRepository _activityRepository;
	private readonly ITimelineRepository _timelineRepository;

	/// <summary>Creates a new database-backed transition provider.</summary>
	public DatabaseActivityTransitionProvider(
		IActivityRepository activityRepository,
		ITimelineRepository timelineRepository)
	{
		_activityRepository = activityRepository;
		_timelineRepository = timelineRepository;
	}

	/// <inheritdoc />
	public async Task<IReadOnlyList<ActivityTransition>> GetTransitionsAsync(
		DateTime startLocalTime, DateTime endLocalTime, CancellationToken cancellationToken = default)
	{
		// Find the Applications timeline
		var timelines = await _timelineRepository.GetTimelinesAsync(cancellationToken).ConfigureAwait(false);
		var appTimeline = timelines.FirstOrDefault(t =>
			string.Equals(t.BaseSchemaName, "ManicTime/Applications", StringComparison.Ordinal));

		if (appTimeline is null)
		{
			return [];
		}

		var startStr = startLocalTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);
		var endStr = endLocalTime.ToString("yyyy-MM-dd HH:mm:ss", System.Globalization.CultureInfo.InvariantCulture);

		var activities = await _activityRepository.GetActivitiesAsync(
			appTimeline.ReportId, startStr, endStr, cancellationToken: cancellationToken).ConfigureAwait(false);

		if (activities.Count < 2)
		{
			return [];
		}

		var transitions = new List<ActivityTransition>();
		for (var i = 1; i < activities.Count; i++)
		{
			var prev = activities[i - 1];
			var curr = activities[i];

			if (!string.Equals(prev.Name, curr.Name, StringComparison.Ordinal))
			{
				transitions.Add(new ActivityTransition
				{
					Timestamp = DateTime.ParseExact(
						curr.StartLocalTime, "yyyy-MM-dd HH:mm:ss",
						System.Globalization.CultureInfo.InvariantCulture),
					FromActivity = prev.Name,
					ToActivity = curr.Name ?? "(unknown)",
				});
			}
		}

		return transitions;
	}
}
