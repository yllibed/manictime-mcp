using AwesomeAssertions;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Models;

namespace ManicTimeMcp.Tests.Configuration;

[TestClass]
public sealed class DataDirectoryResolverTests
{
	[TestMethod]
	public void Resolve_EnvironmentVariable_TakesPrecedenceOverAll()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: @"C:\Custom\ManicTime",
			isWindows: true,
			registryValue: @"C:\Registry\ManicTime",
			localAppDataCandidate: @"C:\LocalAppData\Finkit\ManicTime");

		result.Path.Should().Be(@"C:\Custom\ManicTime");
		result.Source.Should().Be(DataDirectorySource.EnvironmentVariable);
	}

	[TestMethod]
	public void Resolve_EnvironmentVariable_TrimsWhitespace()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: "  /mnt/data/manictime  ",
			isWindows: false,
			registryValue: null,
			localAppDataCandidate: null);

		result.Path.Should().Be("/mnt/data/manictime");
		result.Source.Should().Be(DataDirectorySource.EnvironmentVariable);
	}

	[TestMethod]
	public void Resolve_Windows_RegistryFallback_WhenNoEnvVar()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: null,
			isWindows: true,
			registryValue: @"D:\ManicTimeData",
			localAppDataCandidate: @"C:\Users\test\AppData\Local\Finkit\ManicTime");

		result.Path.Should().Be(@"D:\ManicTimeData");
		result.Source.Should().Be(DataDirectorySource.Registry);
	}

	[TestMethod]
	public void Resolve_Windows_RegistryValue_TrimsWhitespace()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: null,
			isWindows: true,
			registryValue: "  D:\\ManicTimeData  ",
			localAppDataCandidate: null);

		result.Path.Should().Be(@"D:\ManicTimeData");
		result.Source.Should().Be(DataDirectorySource.Registry);
	}

	[TestMethod]
	public void Resolve_Windows_LocalAppDataFallback_WhenNoEnvVarOrRegistry()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: null,
			isWindows: true,
			registryValue: null,
			localAppDataCandidate: @"C:\Users\test\AppData\Local\Finkit\ManicTime");

		result.Path.Should().Be(@"C:\Users\test\AppData\Local\Finkit\ManicTime");
		result.Source.Should().Be(DataDirectorySource.LocalAppData);
	}

	[TestMethod]
	public void Resolve_Windows_Unresolved_WhenAllCandidatesNull()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: null,
			isWindows: true,
			registryValue: null,
			localAppDataCandidate: null);

		result.Path.Should().BeNull();
		result.Source.Should().Be(DataDirectorySource.Unresolved);
	}

	[TestMethod]
	public void Resolve_NonWindows_Unresolved_WhenNoEnvVar()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: null,
			isWindows: false,
			registryValue: null,
			localAppDataCandidate: null);

		result.Path.Should().BeNull();
		result.Source.Should().Be(DataDirectorySource.Unresolved);
	}

	[TestMethod]
	public void Resolve_NonWindows_IgnoresRegistryAndLocalAppData()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: null,
			isWindows: false,
			registryValue: @"C:\ShouldBeIgnored",
			localAppDataCandidate: @"C:\AlsoIgnored");

		result.Path.Should().BeNull();
		result.Source.Should().Be(DataDirectorySource.Unresolved);
	}

	[TestMethod]
	public void Resolve_EmptyEnvVar_FallsThrough()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: "   ",
			isWindows: true,
			registryValue: @"C:\FromRegistry",
			localAppDataCandidate: null);

		result.Path.Should().Be(@"C:\FromRegistry");
		result.Source.Should().Be(DataDirectorySource.Registry);
	}

	[TestMethod]
	public void Resolve_EmptyRegistryValue_FallsThrough()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: null,
			isWindows: true,
			registryValue: "  ",
			localAppDataCandidate: @"C:\LocalAppData\Path");

		result.Path.Should().Be(@"C:\LocalAppData\Path");
		result.Source.Should().Be(DataDirectorySource.LocalAppData);
	}

	[TestMethod]
	public void Resolve_NonWindows_EnvVarStillWorks()
	{
		var result = DataDirectoryResolver.Resolve(
			environmentVariable: "/home/user/manictime-data",
			isWindows: false,
			registryValue: null,
			localAppDataCandidate: null);

		result.Path.Should().Be("/home/user/manictime-data");
		result.Source.Should().Be(DataDirectorySource.EnvironmentVariable);
	}
}
