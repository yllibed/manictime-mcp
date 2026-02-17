namespace ManicTimeMcp.Mcp;

/// <summary>Static guide text for the manictime://guide resource.</summary>
internal static class GuideContent
{
	internal static string Text { get; } = """
		# ManicTime MCP Usage Guide

		## Tool Inventory

		| Tool | Purpose | Best For |
		|---|---|---|
		| get_timelines | List available timelines | Discovery |
		| get_activities | Raw activity data with enriched fields | Drill-down |
		| get_application_usage | App usage from pre-aggregated tables | Usage analysis |
		| get_document_usage | Document usage from pre-aggregated tables | File tracking |
		| get_computer_usage | Computer on/off periods | Availability |
		| get_tags | User-defined tags | Categorization |
		| get_activity_narrative | Structured "what did I do?" | Single-day recap |
		| get_period_summary | Multi-day overview with patterns | Weekly/monthly review |
		| get_website_usage | Website usage with hourly/daily breakdown | Web tracking |
		| list_screenshots | Screenshot metadata (zero bytes) | Discovery |
		| get_screenshot | Single screenshot (dual-audience) | Visual inspection |
		| crop_screenshot | Region crop from screenshot | Detail extraction |

		## Decision Tree

		- "What did I do today/yesterday?" -> get_activity_narrative
		- "How was my week/month?" -> get_period_summary
		- "What websites did I use?" -> get_website_usage
		- "What was I doing at 3pm?" -> get_activities (narrow range) + list_screenshots
		- "Show me screenshots" -> list_screenshots -> get_screenshot -> crop_screenshot
		- "What apps do I use most?" -> get_application_usage

		## Playbooks

		### Daily Recap
		1. get_activity_narrative(startDate=DATE, endDate=DATE+1)
		2. Present segments, top apps, total active time

		### Weekly Recap
		1. get_period_summary(startDate=MONDAY, endDate=NEXT_MONDAY)
		2. Present busiest/quietest days, day-of-week patterns, top apps

		### Screenshot Investigation
		1. list_screenshots(startDate, endDate, samplingStrategy="activity_transition")
		2. get_screenshot(screenshotRef) for the most relevant screenshot
		3. Inspect the thumbnail (model sees it via dual-audience)
		4. If a region needs detail: crop_screenshot(screenshotRef, x, y, width, height)
		5. Combine with get_activity_narrative for context

		### "Why No Screenshots?" Diagnostics
		1. list_screenshots — check diagnostics.reasonCode and remediationHint
		2. Read manictime://health to check screenshot directory status
		3. Read manictime://data-range to verify data exists for the period

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

		- Use resolved display names (from CommonGroup) in user-facing responses
		- Use Color values for visual formatting cues
		- Key values (e.g., "chrome.exe") are internal — use Name for display
		- Keep screenshotRef values for tool chaining only, not user display

		## Screenshot Workflow

		- Always discover via list_screenshots first
		- get_screenshot returns dual-audience: model sees thumbnail, human sees full image
		- crop_screenshot uses percentage coordinates (0-100) by default
		- Coordinates are resolution-independent (same region regardless of thumbnail vs full-size)
		""";
}
