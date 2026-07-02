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
			usage.Map("summary", ManicTimeReplHandlers.ListUsageSummaryAsync)
				.WithDescription("Consolidated usage summary: apps, websites, documents, tags, and total active time.")
				.WithDetails(
					"Returns a single response with top applications, websites, documents, tags, and total active computer time. " +
					"Use --type to filter to a specific section (applications, websites, documents, tags) or omit for all.")
				.ReadOnly();
		});
	}

	private static void MapApplications(IReplMap usage)
	{
		usage.Map("applications", ManicTimeReplHandlers.ListApplicationUsageAsync)
			.WithDescription("Summarize application usage for a date range.")
			.WithDetails(
				"Returns ranked application usage from pre-aggregated tables with total duration per app. " +
				"Use usage summary for a consolidated view across multiple usage sections.")
			.ReadOnly();
	}

	private static void MapDocuments(IReplMap usage)
	{
		usage.Map("documents", ManicTimeReplHandlers.ListDocumentUsageAsync)
			.WithDescription("Summarize document usage for a date range.")
			.WithDetails(
				"Returns ranked file usage from pre-aggregated tables with total duration per document. " +
				"Covers files, not websites; use usage websites for web tracking.")
			.ReadOnly();
	}

	private static void MapWebsites(IReplMap usage)
	{
		usage.Map("websites", ManicTimeReplHandlers.ListWebsiteUsageAsync)
			.WithDescription("Summarize website usage for a date range.")
			.WithDetails(
				"Returns ranked website usage with total duration per domain and hourly or daily breakdown. " +
				"Domains are bare, for example github.com, not full URLs. Use minMinutes to filter noise.")
			.ReadOnly();
	}
}
