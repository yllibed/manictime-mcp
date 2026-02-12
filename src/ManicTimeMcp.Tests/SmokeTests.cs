using AwesomeAssertions;

namespace ManicTimeMcp.Tests;

[TestClass]
public sealed class SmokeTests
{
	[TestMethod]
	public void ProjectAssembly_CanBeLoaded()
	{
		var assembly = typeof(Program).Assembly;

		assembly.Should().NotBeNull();
		assembly.GetName().Name.Should().Be("ManicTimeMcp");
	}
}
