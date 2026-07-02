using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class UsageModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("usage", usage =>
		{
			usage.Map("summary", ManicTimeReplHandlers.ListUsageSummaryAsync)
				.WithDescription("Consolidated usage summary: apps, websites, documents, tags, and total active time.")
				.WithDetails(
					"Returns a single response with top applications, websites, documents, tags, and total active computer time. " +
					"Use --type to filter to a specific section (applications, websites, documents, tags) or omit for all. " +
					"Replaces the need to call separate usage tools. Total active time comes from the ComputerUsage timeline.")
				.ReadOnly();
		});
	}
}
