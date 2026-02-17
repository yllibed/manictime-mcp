using AwesomeAssertions;
using ManicTimeMcp.Database;
using ManicTimeMcp.Models;
using Microsoft.Extensions.Logging.Abstractions;

namespace ManicTimeMcp.Tests.Database;

[TestClass]
public sealed class SchemaValidatorTests
{
	#region Valid schema

	[TestMethod]
	public void Validate_CompleteSchema_ReturnsValid()
	{
		using var fixture = FixtureDatabase.CreateFull();
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Valid);
		result.Issues.Should().BeEmpty();
		result.Capabilities.Should().NotBeNull();
	}

	#endregion

	#region Missing core tables

	[TestMethod]
	public void Validate_MissingTimelineTable_ReturnsInvalid()
	{
		using var fixture = FixtureDatabase.CreatePartial("Ar_Activity", "Ar_Group");
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Invalid);
		result.Issues.Should().Contain(i =>
			i.Code == IssueCode.SchemaValidationFailed &&
			i.Severity == ValidationSeverity.Fatal &&
			i.Message.Contains("Ar_Timeline"));
	}

	[TestMethod]
	public void Validate_MissingActivityTable_ReturnsInvalid()
	{
		using var fixture = FixtureDatabase.CreatePartial("Ar_Timeline", "Ar_Group");
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Invalid);
		result.Issues.Should().Contain(i =>
			i.Code == IssueCode.SchemaValidationFailed &&
			i.Message.Contains("Ar_Activity"));
	}

	[TestMethod]
	public void Validate_MissingGroupTable_ReturnsInvalid()
	{
		using var fixture = FixtureDatabase.CreatePartial("Ar_Timeline", "Ar_Activity");
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Invalid);
		result.Issues.Should().Contain(i =>
			i.Code == IssueCode.SchemaValidationFailed &&
			i.Message.Contains("Ar_Group"));
	}

	[TestMethod]
	public void Validate_AllTablesMissing_ReportsAllMissing()
	{
		using var fixture = FixtureDatabase.CreatePartial(); // empty DB
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Invalid);
		result.Issues.Count.Should().Be(18);
	}

	#endregion

	#region Missing core columns

	[TestMethod]
	public void Validate_MissingSchemaNameColumn_ReturnsInvalid()
	{
		using var fixture = FixtureDatabase.CreateWithMissingColumn("Ar_Timeline", "SchemaName");
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Invalid);
		result.Issues.Should().Contain(i =>
			i.Code == IssueCode.SchemaValidationFailed &&
			i.Message.Contains("SchemaName") &&
			i.Message.Contains("Ar_Timeline"));
	}

	[TestMethod]
	public void Validate_MissingStartLocalTimeColumn_ReturnsInvalid()
	{
		using var fixture = FixtureDatabase.CreateWithMissingColumn("Ar_Activity", "StartLocalTime");
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Invalid);
		result.Issues.Should().Contain(i =>
			i.Code == IssueCode.SchemaValidationFailed &&
			i.Message.Contains("StartLocalTime"));
	}

	[TestMethod]
	public void Validate_MissingNameColumn_ReturnsInvalid()
	{
		using var fixture = FixtureDatabase.CreateWithMissingColumn("Ar_Group", "Name");
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Invalid);
		result.Issues.Should().Contain(i =>
			i.Code == IssueCode.SchemaValidationFailed &&
			i.Message.Contains("Name"));
	}

	#endregion

	#region Tiered validation — supplemental missing = warning

	[TestMethod]
	public void Validate_CoreOnlySchema_ReturnsValidWithWarnings()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly();
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.ValidWithWarnings);
		result.Issues.Should().OnlyContain(i => i.Severity == ValidationSeverity.Warning);
		result.Issues.Should().Contain(i => i.Code == IssueCode.SupplementalTableMissing);
	}

	[TestMethod]
	public void Validate_CoreOnlySchema_CapabilityMatrixAllFalse()
	{
		using var fixture = FixtureDatabase.CreateCoreOnly();
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Capabilities.Should().NotBeNull();
		result.Capabilities!.HasPreAggregatedAppUsage.Should().BeFalse();
		result.Capabilities.HasCommonGroup.Should().BeFalse();
		result.Capabilities.HasTags.Should().BeFalse();
		result.Capabilities.HasTimelineSummary.Should().BeFalse();
		result.Capabilities.HasEnvironment.Should().BeFalse();
	}

	[TestMethod]
	public void Validate_FullSchema_CapabilityMatrixAllTrue()
	{
		using var fixture = FixtureDatabase.CreateFull();
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Capabilities.Should().NotBeNull();
		result.Capabilities!.HasPreAggregatedAppUsage.Should().BeTrue();
		result.Capabilities.HasPreAggregatedWebUsage.Should().BeTrue();
		result.Capabilities.HasPreAggregatedDocUsage.Should().BeTrue();
		result.Capabilities.HasHourlyUsage.Should().BeTrue();
		result.Capabilities.HasCommonGroup.Should().BeTrue();
		result.Capabilities.HasTags.Should().BeTrue();
		result.Capabilities.HasTimelineSummary.Should().BeTrue();
		result.Capabilities.HasEnvironment.Should().BeTrue();
	}

	[TestMethod]
	public void Validate_PartialSupplemental_CapabilityMatrixCorrect()
	{
		using var fixture = FixtureDatabase.CreatePartial(
			"Ar_Timeline", "Ar_Activity", "Ar_Group",
			"Ar_CommonGroup", "Ar_ApplicationByDay");
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.ValidWithWarnings);
		result.Capabilities!.HasPreAggregatedAppUsage.Should().BeTrue();
		result.Capabilities.HasPreAggregatedWebUsage.Should().BeFalse();
		result.Capabilities.HasTags.Should().BeFalse();
	}

	[TestMethod]
	public void Validate_SupplementalTableMissingColumn_CapabilityExcluded()
	{
		// Ar_CommonGroup exists but is missing the 'Name' column.
		// Even though the table is present, the capability should be false.
		using var fixture = FixtureDatabase.CreateFullWithMissingColumn("Ar_CommonGroup", "Name");
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.ValidWithWarnings);
		result.Capabilities!.HasCommonGroup.Should().BeFalse();
		result.Capabilities.HasPreAggregatedAppUsage.Should().BeFalse();
		result.Issues.Should().Contain(i =>
			i.Code == IssueCode.SupplementalColumnMissing &&
			i.Message.Contains("Name") &&
			i.Message.Contains("Ar_CommonGroup"));
	}

	#endregion

	#region Capability matrix

	[TestMethod]
	public void CapabilityMatrix_GetDegradedCapabilities_CoreOnly()
	{
		var matrix = new QueryCapabilityMatrix([]);

		var degraded = matrix.GetDegradedCapabilities();

		degraded.Should().Contain("PreAggregatedAppUsage");
		degraded.Should().Contain("CommonGroup");
		degraded.Should().Contain("Tags");
		degraded.Should().Contain("TimelineSummary");
		degraded.Should().Contain("Environment");
	}

	[TestMethod]
	public void CapabilityMatrix_GetDegradedCapabilities_FullSchema_Empty()
	{
		using var fixture = FixtureDatabase.CreateFull();
		var sut = CreateValidator();
		var result = sut.Validate(fixture.FilePath);

		var degraded = result.Capabilities!.GetDegradedCapabilities();

		degraded.Should().BeEmpty();
	}

	#endregion

	#region Helpers

	private static SchemaValidator CreateValidator() =>
		new(NullLogger<SchemaValidator>.Instance);

	#endregion
}
