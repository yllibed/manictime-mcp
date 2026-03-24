using AwesomeAssertions;
using ManicTimeMcp.Repl;
using Repl.Mcp;

namespace ManicTimeMcp.Tests.Repl;

[TestClass]
public sealed class NullMcpClientRootsTests
{
	[TestMethod]
	public async Task SetSoftRoots_PersistsRootsForLaterReads()
	{
		var roots = new NullMcpClientRoots();
		var expectedRoot = new McpClientRoot(new Uri(@"file:///D:/reports/weekly-recap/"), "weekly-recap");

		roots.SetSoftRoots([expectedRoot]);

		roots.IsSupported.Should().BeFalse();
		roots.HasSoftRoots.Should().BeTrue();
		roots.Current.Should().ContainSingle();
		roots.Current[0].Uri.Should().Be(expectedRoot.Uri);
		roots.Current[0].Name.Should().Be(expectedRoot.Name);

		var resolved = await roots.GetAsync().ConfigureAwait(false);
		resolved.Should().ContainSingle();
		resolved[0].Uri.Should().Be(expectedRoot.Uri);
	}

	[TestMethod]
	public void ClearSoftRoots_RemovesSessionRoots()
	{
		var roots = new NullMcpClientRoots();
		roots.SetSoftRoots([new McpClientRoot(new Uri(@"file:///D:/reports/weekly-recap/"), "weekly-recap")]);

		roots.ClearSoftRoots();

		roots.HasSoftRoots.Should().BeFalse();
		roots.Current.Should().BeEmpty();
	}
}
