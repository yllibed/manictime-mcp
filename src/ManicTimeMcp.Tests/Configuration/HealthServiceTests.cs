using AwesomeAssertions;
using ManicTimeMcp.Configuration;
using ManicTimeMcp.Database;
using ManicTimeMcp.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManicTimeMcp.Tests.Configuration;

[TestClass]
public sealed class HealthServiceTests
{
	private const string TestDataDir = @"C:\TestManicTime";
	private const string TestDbPath = @"C:\TestManicTime\ManicTimeReports.db";
	private const string TestScreenshotDir = @"C:\TestManicTime\Screenshots";

	#region DeriveStatus

	[TestMethod]
	public void DeriveStatus_NoIssues_ReturnsHealthy()
	{
		var status = HealthService.DeriveStatus([]);

		status.Should().Be(HealthStatus.Healthy);
	}

	[TestMethod]
	public void DeriveStatus_OnlyWarnings_ReturnsDegraded()
	{
		var issues = new List<ValidationIssue>
		{
			new()
			{
				Code = IssueCode.ManicTimeProcessNotRunning,
				Severity = ValidationSeverity.Warning,
				Message = "test",
			},
		};

		HealthService.DeriveStatus(issues).Should().Be(HealthStatus.Degraded);
	}

	[TestMethod]
	public void DeriveStatus_AnyFatal_ReturnsUnhealthy()
	{
		var issues = new List<ValidationIssue>
		{
			new()
			{
				Code = IssueCode.ManicTimeProcessNotRunning,
				Severity = ValidationSeverity.Warning,
				Message = "warning",
			},
			new()
			{
				Code = IssueCode.DatabaseNotFound,
				Severity = ValidationSeverity.Fatal,
				Message = "fatal",
			},
		};

		HealthService.DeriveStatus(issues).Should().Be(HealthStatus.Unhealthy);
	}

	#endregion

	#region GetHealthReport — fully healthy

	[TestMethod]
	public void GetHealthReport_FullyHealthy_ReturnsHealthyWithNoIssues()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 1024 * 1024 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.Status.Should().Be(HealthStatus.Healthy);
		report.DataDirectory.Should().Be(TestDataDir);
		report.DirectorySource.Should().Be(DataDirectorySource.EnvironmentVariable);
		report.DatabaseExists.Should().BeTrue();
		report.DatabaseSizeBytes.Should().Be(1024 * 1024);
		report.SchemaStatus.Should().Be(SchemaValidationStatus.Valid);
		report.ManicTimeProcessRunning.Should().BeTrue();
		report.Screenshots.Status.Should().Be(ScreenshotAvailabilityStatus.Available);
		report.Screenshots.Reason.Should().Be(ScreenshotUnavailableReason.None);
		report.Issues.Should().BeEmpty();
	}

	#endregion

	#region GetHealthReport — data directory unresolved

	[TestMethod]
	public void GetHealthReport_UnresolvedDirectory_ReturnsUnhealthyWithFatalIssue()
	{
		var resolver = new StubResolver(path: null, DataDirectorySource.Unresolved);
		var platform = new FakePlatformEnvironment();

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.Status.Should().Be(HealthStatus.Unhealthy);
		report.DataDirectory.Should().BeNull();
		report.DirectorySource.Should().Be(DataDirectorySource.Unresolved);
		report.DatabaseExists.Should().BeFalse();
		report.DatabaseSizeBytes.Should().BeNull();
		report.Screenshots.Status.Should().Be(ScreenshotAvailabilityStatus.Unknown);
		report.Issues.Should().ContainSingle(i =>
			i.Code == IssueCode.DataDirectoryUnresolved &&
			i.Severity == ValidationSeverity.Fatal);
	}

	#endregion

	#region GetHealthReport — database missing

	[TestMethod]
	public void GetHealthReport_DatabaseMissing_ReturnsFatalIssue()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.LocalAppData);
		var platform = new FakePlatformEnvironment
		{
			// DB file does not exist
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.Status.Should().Be(HealthStatus.Unhealthy);
		report.DatabaseExists.Should().BeFalse();
		report.DatabaseSizeBytes.Should().BeNull();
		report.Issues.Should().Contain(i =>
			i.Code == IssueCode.DatabaseNotFound &&
			i.Severity == ValidationSeverity.Fatal);
	}

	#endregion

	#region GetHealthReport — process not running

	[TestMethod]
	public void GetHealthReport_ProcessNotRunning_ReturnsWarning()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 500 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			// ManicTime process NOT running
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.Status.Should().Be(HealthStatus.Degraded);
		report.ManicTimeProcessRunning.Should().BeFalse();
		report.Issues.Should().Contain(i =>
			i.Code == IssueCode.ManicTimeProcessNotRunning &&
			i.Severity == ValidationSeverity.Warning);
	}

	#endregion

	#region GetHealthReport — screenshot directory absent

	[TestMethod]
	public void GetHealthReport_ScreenshotDirectoryAbsent_ReturnsWarningWithCaptureDisabled()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.Registry);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 100 },
			RunningProcesses = { HealthService.ManicTimeProcessName },
			// Screenshot directory does NOT exist
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.Screenshots.Status.Should().Be(ScreenshotAvailabilityStatus.Unavailable);
		report.Screenshots.Reason.Should().Be(ScreenshotUnavailableReason.CaptureDisabled);
		report.Screenshots.RemediationHint.Should().NotBeNullOrEmpty();
		report.Issues.Should().Contain(i => i.Code == IssueCode.ScreenshotDirectoryAbsent);
	}

	#endregion

	#region GetHealthReport — screenshot directory empty

	[TestMethod]
	public void GetHealthReport_ScreenshotDirectoryEmpty_ReturnsWarningWithRetention()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.Registry);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 100 },
			ExistingDirectories = { TestScreenshotDir },
			// DirectoriesWithFiles does NOT include screenshot dir — empty
			RunningProcesses = { HealthService.ManicTimeProcessName },
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.Screenshots.Status.Should().Be(ScreenshotAvailabilityStatus.Unavailable);
		report.Screenshots.Reason.Should().Be(ScreenshotUnavailableReason.Retention);
		report.Issues.Should().Contain(i => i.Code == IssueCode.ScreenshotDirectoryEmpty);
	}

	#endregion

	#region GetHealthReport — multiple issues compound correctly

	[TestMethod]
	public void GetHealthReport_MultipleFatalAndWarning_ReportsAll()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			// DB missing → fatal
			// Process not running → warning
			// Screenshots dir absent → warning
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.Status.Should().Be(HealthStatus.Unhealthy);
		report.Issues.Count.Should().BeGreaterThanOrEqualTo(3);
		report.Issues.Should().Contain(i => i.Code == IssueCode.DatabaseNotFound);
		report.Issues.Should().Contain(i => i.Code == IssueCode.ManicTimeProcessNotRunning);
		report.Issues.Should().Contain(i => i.Code == IssueCode.ScreenshotDirectoryAbsent);
	}

	#endregion

	#region Helpers

	private static HealthService CreateService(IDataDirectoryResolver resolver, IPlatformEnvironment platform, ISchemaValidator? schemaValidator = null) =>
		new(resolver, platform, schemaValidator ?? new StubSchemaValidator(), NullLogger<HealthService>.Instance);

	private sealed class StubSchemaValidator : ISchemaValidator
	{
		public SchemaValidationResult Result { get; set; } = new()
		{
			Status = SchemaValidationStatus.Valid,
			Issues = [],
		};

		public SchemaValidationResult Validate(string databasePath) => Result;
	}

	private sealed class StubResolver(string? path, DataDirectorySource source) : IDataDirectoryResolver
	{
		public DataDirectoryResult Resolve() => new()
		{
			Path = path,
			Source = source,
		};
	}

	private sealed class FakePlatformEnvironment : IPlatformEnvironment
	{
		public HashSet<string> ExistingFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
		public Dictionary<string, long> FileSizes { get; } = new(StringComparer.OrdinalIgnoreCase);
		public HashSet<string> ExistingDirectories { get; } = new(StringComparer.OrdinalIgnoreCase);
		public HashSet<string> DirectoriesWithFiles { get; } = new(StringComparer.OrdinalIgnoreCase);
		public HashSet<string> RunningProcesses { get; } = new(StringComparer.OrdinalIgnoreCase);

		public bool FileExists(string path) => ExistingFiles.Contains(path);

		public long GetFileSize(string path) =>
			FileSizes.TryGetValue(path, out var size) ? size : 0;

		public bool DirectoryExists(string path) => ExistingDirectories.Contains(path);

		public bool DirectoryHasFiles(string path, string searchPattern, SearchOption searchOption) =>
			DirectoriesWithFiles.Contains(path);

		public bool IsProcessRunning(string processName) =>
			RunningProcesses.Contains(processName);
	}

	#endregion
}
