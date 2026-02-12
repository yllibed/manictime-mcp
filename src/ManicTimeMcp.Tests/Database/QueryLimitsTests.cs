using AwesomeAssertions;
using ManicTimeMcp.Database;

namespace ManicTimeMcp.Tests.Database;

[TestClass]
public sealed class QueryLimitsTests
{
	[TestMethod]
	public void Clamp_NullRequested_ReturnsDefault()
	{
		QueryLimits.Clamp(requested: null, defaultLimit: 100, hardCap: 500).Should().Be(100);
	}

	[TestMethod]
	public void Clamp_RequestedBelowCap_ReturnsRequested()
	{
		QueryLimits.Clamp(requested: 50, defaultLimit: 100, hardCap: 500).Should().Be(50);
	}

	[TestMethod]
	public void Clamp_RequestedAboveCap_ReturnsCap()
	{
		QueryLimits.Clamp(requested: 10000, defaultLimit: 100, hardCap: 500).Should().Be(500);
	}

	[TestMethod]
	public void Clamp_RequestedEqualsCap_ReturnsCap()
	{
		QueryLimits.Clamp(requested: 500, defaultLimit: 100, hardCap: 500).Should().Be(500);
	}

	[TestMethod]
	public void Clamp_DefaultAboveCap_ReturnsCap()
	{
		QueryLimits.Clamp(requested: null, defaultLimit: 1000, hardCap: 500).Should().Be(500);
	}
}
