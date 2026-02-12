using ManicTimeMcp.Configuration;
using ManicTimeMcp.Models;

namespace ManicTimeMcp.Tests.Mcp;

internal sealed class StubDataDirectoryResolver(string? path = null) : IDataDirectoryResolver
{
	public DataDirectoryResult Resolve() => new()
	{
		Path = path,
		Source = path is not null ? DataDirectorySource.LocalAppData : DataDirectorySource.Unresolved,
	};
}
