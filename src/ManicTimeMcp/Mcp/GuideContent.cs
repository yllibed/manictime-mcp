namespace ManicTimeMcp.Mcp;

/// <summary>Static guide text for the manictime://resource/guide resource.</summary>
internal static class GuideContent
{
	internal static string Text { get; } = """
		# ManicTime MCP Usage Guide

		## Getting Started

		1. Read manictime://resource/health to check server status and capabilities.
		   - healthy: all checks passed.
		   - potentiallyDegraded: informational issues (e.g. untested ManicTime version); functionality believed intact.
		   - degraded: reduced functionality (e.g. ManicTime process not running, screenshots unavailable).
		   - unhealthy: fatal issues; server cannot operate normally.
		2. Read manictime://resource/data-range to discover available date boundaries per timeline.
		3. Read manictime://resource/environment for device and OS context.
		4. Use the decision tree below to pick the right tool for the user's question.

		## Tool Inventory

		| Repl Route | MCP Tool | Purpose | Best For |
		|---|---|---|---|
		| timeline list | timeline_list | List available timelines | Discovery |
		| activity list | activity_list | Raw activity data with enriched fields | Drill-down |
		| activity computer-usage | activity_computer-usage | Computer on/off periods | Availability |
		| usage summary | usage_summary | Apps, websites, documents, tags, and active time | Usage analysis |
		| summary narrative | summary_narrative | Structured "what did I do?" | Single-day recap |
		| summary period | summary_period | Multi-day overview with patterns | Weekly/monthly review |
		| summary daily | summary_daily | Daily activity summary (single-call recap) | Quick daily overview |
		| screenshot list | screenshot_list | Screenshot metadata (zero image bytes) | Discovery |
		| screenshot get | screenshot_get | Single screenshot payload | Visual inspection |
		| screenshot crop | screenshot_crop | Region crop from screenshot | Detail extraction |
		| screenshot save | screenshot_save | Save a screenshot to disk (within MCP roots) | Report assets |

		## Decision Tree

		- "What did I do today/yesterday?" -> summary narrative (`summary_narrative` in MCP) and check suggested screenshots for visual context.
		- "How was my week/month?" -> summary period (`summary_period` in MCP).
		- "What websites did I use?" -> usage summary with type websites (`usage_summary` in MCP).
		- "What was I doing at 3pm?" -> activity list (`activity_list` in MCP) for a narrow period, then screenshot list.
		- "Show me screenshots" -> screenshot list -> screenshot get -> screenshot crop -> screenshot save.
		- "What apps do I use most?" -> usage summary with type applications (`usage_summary` in MCP).

		## Playbooks

		### Daily Recap
		1. Run summary daily (`summary_daily` in MCP) for the target date with hourly web detail enabled.
		2. If suggested screenshots are present, call screenshot get for 2-3 of them.
		3. Inspect each thumbnail and use screenshot crop to extract the active window or focused content. Crops are sharper and more meaningful for reports.
		4. Use screenshot save to persist the best crops to the project assets folder for embedding in markdown reports.
		5. Present segments, top apps, total active time, and the best cropped visuals.

		### Weekly Recap
		1. Run summary period (`summary_period` in MCP) for the selected Repl date range.
		2. Present busiest and quietest days, repeated patterns, and top apps/websites.

		### Screenshot Investigation
		1. Run screenshot list (`screenshot_list` in MCP) for the relevant Repl date-time window.
		2. Fetch the most relevant screenshot with screenshot get.
		3. Inspect the image payload or thumbnail preview.
		4. If a region needs detail, run screenshot crop with the region coordinates.
		5. Combine the screenshot evidence with summary narrative for surrounding activity context.

		### "Why No Screenshots?" Diagnostics
		1. Run screenshot list and inspect diagnostics and truncation data.
		2. Read manictime://resource/health to check screenshot directory status and overall health.
		3. Read manictime://resource/data-range to verify that data exists for the period.
		4. Read manictime://resource/config to confirm the data directory is resolved correctly.
		5. Read manictime://resource/environment for device context if the issue is environment-specific.

		## Data Model

		- **Timelines**: Data sources (Applications, Documents, Computer Usage, etc.)
		- **Activities**: Time spans with start/end and associated metadata
		- **Groups**: Categories within a timeline (e.g., "Chrome" in Applications)
		- **CommonGroup**: Cross-timeline resolved names and colors
		- **Pre-aggregated tables**: Ar_ApplicationByDay, Ar_WebSiteByDay, etc. — faster queries

		## Date/Time Semantics

		- startDate: inclusive (>= start)
		- endDate: exclusive (< end)
		- Date-only values expand to local T00:00:00
		- All times are local time (no timezone conversion)

		## Communication Guidance

		- When suggested screenshots are provided, fetch 2-3 with screenshot get, then use screenshot crop to extract the active window or key content region. Include these crops in your response as visual anchors because they are much more readable than full-screen thumbnails.
		- Use resolved display names from CommonGroup in user-facing responses.
		- Use Color values for visual formatting cues when available.
		- Key values such as `chrome.exe` are internal identifiers. Use Name for display.
		- Keep screenshotRef values for tool chaining only, not user display.

		## Screenshot Workflow

		- Always discover screenshots via screenshot list first.
		- screenshot get returns metadata plus image payloads encoded as base64 text.
		- screenshot crop uses percentage coordinates (0-100) by default.
		- Coordinates are resolution-independent, so the same region works regardless of thumbnail vs full-size rendering.
		- screenshot save writes to disk within MCP client roots and requires roots support from the client.
		- Full pipeline: screenshot list -> screenshot get -> screenshot crop -> screenshot save.
		""";
}
