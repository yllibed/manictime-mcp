using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class UsageModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("usage", usage =>
		{
			MapApplications(usage);
			MapDocuments(usage);
			MapWebsites(usage);
		});
	}

	private static void MapApplications(IReplMap usage)
	{
		usage.Map("applications", ManicTimeReplHandlers.ListApplicationUsageAsync)
			.WithDescription("Summarize application usage for a date range.")
			.WithDetails(
				"Returns ranked application usage from pre-aggregated tables with total duration per app. " +
				"Faster than activity list for answering 'what apps did I use most?' questions.")
			.ReadOnly();
	}

	private static void MapDocuments(IReplMap usage)
	{
		usage.Map("documents", ManicTimeReplHandlers.ListDocumentUsageAsync)
			.WithDescription("Summarize document usage for a date range.")
			.WithDetails(
				"Returns ranked file usage from pre-aggregated tables with total duration per document. " +
				"Covers files, not websites — use usage websites for web tracking.")
			.ReadOnly();
	}

	private static void MapWebsites(IReplMap usage)
	{
		usage.Map("websites", ManicTimeReplHandlers.ListWebsiteUsageAsync)
			.WithDescription("Summarize website usage for a date range.")
			.WithDetails(
				"Returns ranked website usage with total duration per domain and optional hourly breakdown. " +
				"Domains are bare (e.g. 'github.com'), not full URLs. Use minMinutes to filter noise.")
			.ReadOnly();
	}
}
