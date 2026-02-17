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
	private const string TestInstallDir = @"C:\Program Files\ManicTime\";
	private const string TestExePath = @"C:\Program Files\ManicTime\ManicTime.exe";
	private const string TestVersion = "2025.3.5.0";
	private const int TestProcessId = 12345;

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
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
			ManicTimeInstallDir = TestInstallDir,
			FileProductVersions = { [TestExePath] = TestVersion },
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
		report.ManicTimeProcessId.Should().Be(TestProcessId);
		report.ManicTimeVersion.Should().Be(TestVersion);
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
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
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
		report.ManicTimeProcessId.Should().BeNull();
		report.Issues.Should().Contain(i =>
			i.Code == IssueCode.ManicTimeProcessNotRunning &&
			i.Severity == ValidationSeverity.Warning);
	}

	#endregion

	#region GetHealthReport — version detection

	[TestMethod]
	public void GetHealthReport_ManicTimeInstalled_ReportsVersion()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 500 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
			ProcessIds = { [HealthService.ManicTimeProcessName] = 9999 },
			ManicTimeInstallDir = TestInstallDir,
			FileProductVersions = { [TestExePath] = TestVersion },
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.ManicTimeVersion.Should().Be(TestVersion);
	}

	[TestMethod]
	public void GetHealthReport_ManicTimeNotInstalled_ReportsNullVersion()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 500 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
			ProcessIds = { [HealthService.ManicTimeProcessName] = 9999 },
			// No ManicTimeInstallDir set
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.ManicTimeVersion.Should().BeNull();
	}

	#endregion

	#region GetHealthReport — version compatibility

	[TestMethod]
	public void GetHealthReport_MatchingVersion_NoUntestedIssue()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 500 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
			ManicTimeInstallDir = TestInstallDir,
			FileProductVersions = { [TestExePath] = TestVersion },
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.Issues.Should().NotContain(i => i.Code == IssueCode.ManicTimeVersionUntested);
		report.TestedManicTimeVersion.Should().Be(HealthService.TestedManicTimeVersion);
	}

	[TestMethod]
	public void GetHealthReport_DifferentVersion_EmitsUntestedWarning()
	{
		const string differentVersion = "2025.3.8.0";
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 500 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
			ManicTimeInstallDir = TestInstallDir,
			FileProductVersions = { [TestExePath] = differentVersion },
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.ManicTimeVersion.Should().Be(differentVersion);
		report.TestedManicTimeVersion.Should().Be(HealthService.TestedManicTimeVersion);
		var issue = report.Issues.Should().ContainSingle(i => i.Code == IssueCode.ManicTimeVersionUntested).Subject;
		issue.Severity.Should().Be(ValidationSeverity.Warning);
		issue.Message.Should().Contain(differentVersion);
		issue.Message.Should().Contain(HealthService.TestedManicTimeVersion);
	}

	[TestMethod]
	public void GetHealthReport_VersionNotDetected_NoUntestedIssue()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 500 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
			// No ManicTimeInstallDir — version will be null
		};

		var sut = CreateService(resolver, platform);
		var report = sut.GetHealthReport();

		report.ManicTimeVersion.Should().BeNull();
		report.TestedManicTimeVersion.Should().Be(HealthService.TestedManicTimeVersion);
		report.Issues.Should().NotContain(i => i.Code == IssueCode.ManicTimeVersionUntested);
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
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
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
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
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

	#region GetHealthReport — degraded capabilities

	[TestMethod]
	public void GetHealthReport_AllSupplementalPresent_AllCapabilitiesAvailable()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 1024 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
		};

		var sut = CreateService(resolver, platform, capabilities: CreateFullCapabilities());
		var report = sut.GetHealthReport();

		report.Capabilities.Should().NotBeNull();
		report.Capabilities!.Should().OnlyContain(c => c.Available);
	}

	[TestMethod]
	public void GetHealthReport_NoSupplementalTables_ReportsCapabilitiesWithFallback()
	{
		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 1024 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
		};

		var sut = CreateService(resolver, platform, capabilities: CreateCoreOnlyCapabilities());
		var report = sut.GetHealthReport();

		report.Capabilities.Should().NotBeNull();
		var unavailable = report.Capabilities!.Where(c => !c.Available).ToList();
		unavailable.Should().NotBeEmpty();
		unavailable.Select(c => c.Name).Should().Contain("PreAggregatedAppUsage");
		unavailable.Select(c => c.Name).Should().Contain("Tags");
		unavailable.Select(c => c.Name).Should().Contain("Environment");
		// Tags has no fallback, but app/web/doc usage do
		var tags = unavailable.Single(c => string.Equals(c.Name, "Tags", StringComparison.Ordinal));
		tags.FallbackActive.Should().BeFalse();
		var appUsage = unavailable.Single(c => string.Equals(c.Name, "PreAggregatedAppUsage", StringComparison.Ordinal));
		appUsage.FallbackActive.Should().BeTrue();
	}

	[TestMethod]
	public void GetHealthReport_PopulatesCapabilityMatrixFromValidation()
	{
		// Start with an empty (fully degraded) capability matrix — mimics DI startup state
		var capabilities = CreateCoreOnlyCapabilities();
		capabilities.HasPreAggregatedAppUsage.Should().BeFalse("precondition: starts degraded");

		// Stub validator returns capabilities with Ar_CommonGroup + Ar_ApplicationByDay present
		var validator = new StubSchemaValidator
		{
			Result = new()
			{
				Status = SchemaValidationStatus.Valid,
				Issues = [],
				Capabilities = new QueryCapabilityMatrix([
					"Ar_CommonGroup", "Ar_ApplicationByDay", "Ar_WebSiteByDay",
					"Ar_Tag", "Ar_ActivityTag",
				]),
			},
		};

		var resolver = new StubResolver(TestDataDir, DataDirectorySource.EnvironmentVariable);
		var platform = new FakePlatformEnvironment
		{
			ExistingFiles = { TestDbPath },
			FileSizes = { [TestDbPath] = 1024 },
			ExistingDirectories = { TestScreenshotDir },
			DirectoriesWithFiles = { TestScreenshotDir },
			RunningProcesses = { HealthService.ManicTimeProcessName },
			ProcessIds = { [HealthService.ManicTimeProcessName] = TestProcessId },
		};

		var sut = CreateService(resolver, platform, schemaValidator: validator, capabilities: capabilities);
		_ = sut.GetHealthReport();

		// After health check, the DI singleton should be populated
		capabilities.HasPreAggregatedAppUsage.Should().BeTrue("populated from validation");
		capabilities.HasTags.Should().BeTrue("populated from validation");
		capabilities.HasHourlyUsage.Should().BeFalse("Ar_ActivityByHour was not in validated tables");
	}

	#endregion

	#region Helpers

	private static HealthService CreateService(IDataDirectoryResolver resolver, IPlatformEnvironment platform, ISchemaValidator? schemaValidator = null, QueryCapabilityMatrix? capabilities = null) =>
		new(resolver, platform, schemaValidator ?? new StubSchemaValidator(), capabilities ?? CreateFullCapabilities(), NullLogger<HealthService>.Instance);

	/// <summary>Creates a capability matrix where all supplemental tables are present.</summary>
	private static QueryCapabilityMatrix CreateFullCapabilities() =>
		new(SchemaManifest.Tables.Values
			.Where(t => t.Tier != TableTier.Core)
			.Select(t => t.TableName));

	/// <summary>Creates a capability matrix where no supplemental tables are present.</summary>
	private static QueryCapabilityMatrix CreateCoreOnlyCapabilities() => new([]);

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
		public Dictionary<string, int> ProcessIds { get; } = new(StringComparer.OrdinalIgnoreCase);
		public string? ManicTimeInstallDir { get; set; }
		public Dictionary<string, string> FileProductVersions { get; } = new(StringComparer.OrdinalIgnoreCase);

		public bool FileExists(string path) => ExistingFiles.Contains(path);

		public long GetFileSize(string path) =>
			FileSizes.TryGetValue(path, out var size) ? size : 0;

		public bool DirectoryExists(string path) => ExistingDirectories.Contains(path);

		public bool DirectoryHasFiles(string path, string searchPattern, SearchOption searchOption) =>
			DirectoriesWithFiles.Contains(path);

		public bool IsProcessRunning(string processName) =>
			RunningProcesses.Contains(processName);

		public int? GetProcessId(string processName) =>
			ProcessIds.TryGetValue(processName, out var pid) ? pid : null;

		string? IPlatformEnvironment.GetManicTimeInstallDir() => ManicTimeInstallDir;

		public string? GetFileProductVersion(string filePath) =>
			FileProductVersions.TryGetValue(filePath, out var version) ? version : null;
	}

	#endregion
}
