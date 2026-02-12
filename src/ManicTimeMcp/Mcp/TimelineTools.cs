using System.ComponentModel;
using System.Text.Json;
using ManicTimeMcp.Database;
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
	public async Task<string> GetTimelinesAsync(CancellationToken cancellationToken)
	{
		var timelines = await _timelineRepository.GetTimelinesAsync(cancellationToken).ConfigureAwait(false);
		return JsonSerializer.Serialize(timelines, JsonOptions.Default);
	}
#pragma warning restore IL2026
}
