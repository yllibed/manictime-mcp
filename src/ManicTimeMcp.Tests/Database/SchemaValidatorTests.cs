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
		using var fixture = FixtureDatabase.CreateStandard();
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Valid);
		result.Issues.Should().BeEmpty();
	}

	#endregion

	#region Missing tables

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
		result.Issues.Count.Should().Be(3);
	}

	#endregion

	#region Missing columns

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
	public void Validate_MissingParentGroupIdColumn_ReturnsInvalid()
	{
		using var fixture = FixtureDatabase.CreateWithMissingColumn("Ar_Group", "ParentGroupId");
		var sut = CreateValidator();

		var result = sut.Validate(fixture.FilePath);

		result.Status.Should().Be(SchemaValidationStatus.Invalid);
		result.Issues.Should().Contain(i =>
			i.Code == IssueCode.SchemaValidationFailed &&
			i.Message.Contains("ParentGroupId"));
	}

	#endregion

	#region Helpers

	private static SchemaValidator CreateValidator() =>
		new(NullLogger<SchemaValidator>.Instance);

	#endregion
}
