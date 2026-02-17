namespace ManicTimeMcp.Screenshots;

/// <summary>
/// Selects screenshots nearest to activity transitions (application switches).
/// For each transition, picks the screenshot with the closest timestamp.
/// </summary>
internal static class ActivityTransitionSampler
{
	/// <summary>Maximum time gap between a transition and its nearest screenshot to be considered relevant.</summary>
	private static readonly TimeSpan MaxProximity = TimeSpan.FromMinutes(2);

	/// <summary>
	/// Selects screenshots closest to each transition.
	/// Returns one screenshot per distinct transition, ordered by timestamp.
	/// </summary>
	internal static List<ScreenshotInfo> Sample(
		IReadOnlyList<ScreenshotInfo> candidates,
		IReadOnlyList<ActivityTransition> transitions)
	{
		if (candidates.Count == 0 || transitions.Count == 0)
		{
			return [];
		}

		var selected = new HashSet<int>(); // indices into candidates
		foreach (var transition in transitions)
		{
			var bestIdx = FindNearest(candidates, transition.Timestamp);
			if (bestIdx >= 0)
			{
				var gap = Math.Abs((candidates[bestIdx].LocalTimestamp - transition.Timestamp).TotalSeconds);
				if (gap <= MaxProximity.TotalSeconds)
				{
					selected.Add(bestIdx);
				}
			}
		}

		return selected
			.Order()
			.Select(i => candidates[i])
			.ToList();
	}

	private static int FindNearest(IReadOnlyList<ScreenshotInfo> sorted, DateTime target)
	{
		var lo = 0;
		var hi = sorted.Count - 1;

		while (lo <= hi)
		{
			var mid = lo + (hi - lo) / 2;
			if (sorted[mid].LocalTimestamp < target)
			{
				lo = mid + 1;
			}
			else
			{
				hi = mid - 1;
			}
		}

		// lo is the first index >= target; check lo and lo-1 for closest
		var bestIdx = -1;
		var bestDist = double.MaxValue;

		if (lo < sorted.Count)
		{
			var dist = Math.Abs((sorted[lo].LocalTimestamp - target).TotalSeconds);
			if (dist < bestDist)
			{
				bestDist = dist;
				bestIdx = lo;
			}
		}

		if (lo - 1 >= 0)
		{
			var dist = Math.Abs((sorted[lo - 1].LocalTimestamp - target).TotalSeconds);
			if (dist < bestDist)
			{
				bestIdx = lo - 1;
			}
		}

		return bestIdx;
	}
}
