using AwesomeAssertions;
using ManicTimeMcp.Database;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManicTimeMcp.Tests.Database;

[TestClass]
public sealed class EnvironmentRepositoryTests
{
	[TestMethod]
	public async Task GetEnvironmentsAsync_ReturnsSeededData()
	{
		using var fixture = FixtureDatabase.CreateFull(FixtureSeeder.SeedFullData);
		var sut = CreateRepository(fixture);

		var results = await sut.GetEnvironmentsAsync().ConfigureAwait(false);

		results.Should().ContainSingle().Which.DeviceName.Should().Be("WORKSTATION-01");
	}

	[TestMethod]
	public async Task GetEnvironmentsAsync_EmptyTable_ReturnsEmpty()
	{
		using var fixture = FixtureDatabase.CreateFull();
		var sut = CreateRepository(fixture);

		var results = await sut.GetEnvironmentsAsync().ConfigureAwait(false);

		results.Should().BeEmpty();
	}

	private static EnvironmentRepository CreateRepository(FixtureDatabase fixture)
	{
		var factory = new FixtureConnectionFactory(fixture.FilePath);
		return new EnvironmentRepository(factory, NullLogger<EnvironmentRepository>.Instance);
	}
}
