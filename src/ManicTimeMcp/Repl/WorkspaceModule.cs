using Repl;

namespace ManicTimeMcp.Repl;

internal sealed class WorkspaceModule : IReplModule
{
	public void Map(IReplMap map)
	{
		map.Context("workspace", workspace =>
		{
			workspace.Map("init {path}", ManicTimeReplHandlers.InitializeWorkspaceRoots)
				.WithDescription("Initialize soft MCP roots for clients that do not support native roots.")
				.WithDetails("Call this before screenshot save when the MCP client does not expose native roots.")
				.Idempotent();
		});
	}
}
