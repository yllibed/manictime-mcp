using System.ComponentModel;
using System.Text.Json;
using ManicTimeMcp.Database;
using Microsoft.Data.Sqlite;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace ManicTimeMcp.Mcp;

/// <summary>MCP tools for querying ManicTime timelines.</summary>
[McpServerToolType]
public sealed class TimelineTools
{
	private readonly ITimelineRepository _timelineRepository;

	/// <summary>Creates timeline tools with injected repository.</summary>
	public TimelineTools(ITimelineRepository timelineRepository)
	{
		_timelineRepository = timelineRepository;
	}

	/// <summary>Returns all available ManicTime timelines.</summary>
	[McpServerTool(Name = "get_timelines", ReadOnly = true), Description("List all available ManicTime timelines with their schema types.")]
#pragma warning disable IL2026 // Trimming is disabled (PublishTrimmed=false); reflection-based JSON is safe
	public async Task<CallToolResult> GetTimelinesAsync(CancellationToken cancellationToken)
	{
		try
		{
			var timelines = await _timelineRepository.GetTimelinesAsync(cancellationToken).ConfigureAwait(false);
			return ToolResults.Success(JsonSerializer.Serialize(new
			{
				count = timelines.Count,
				timelines,
				truncation = new TruncationInfo
				{
					Truncated = false,
					ReturnedCount = timelines.Count,
					TotalAvailable = timelines.Count,
				},
				diagnostics = DiagnosticsInfo.Ok,
			}, JsonOptions.Default));
		}
		catch (SqliteException ex)
		{
			return ToolResults.Error($"Database error: {ex.Message}. Try reading the manictime://health resource to diagnose the issue.");
		}
		catch (InvalidOperationException ex)
		{
			return ToolResults.Error($"Database is busy after retries: {ex.Message}. ManicTime may be performing a long write operation.");
		}
	}
#pragma warning restore IL2026
}
