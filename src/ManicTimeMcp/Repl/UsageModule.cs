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
			.ReadOnly();
	}

	private static void MapDocuments(IReplMap usage)
	{
		usage.Map("documents", ManicTimeReplHandlers.ListDocumentUsageAsync)
			.WithDescription("Summarize document usage for a date range.")
			.ReadOnly();
	}

	private static void MapWebsites(IReplMap usage)
	{
		usage.Map("websites", ManicTimeReplHandlers.ListWebsiteUsageAsync)
			.WithDescription("Summarize website usage for a date range.")
			.ReadOnly();
	}
}
