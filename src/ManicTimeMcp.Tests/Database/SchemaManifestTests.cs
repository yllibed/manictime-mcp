using AwesomeAssertions;
using ManicTimeMcp.Database;

namespace ManicTimeMcp.Tests.Database;

[TestClass]
public sealed class SchemaManifestTests
{
	[TestMethod]
	public void Tables_ContainsExpectedTableCount()
	{
		SchemaManifest.Tables.Count.Should().Be(18);
	}

	[TestMethod]
	public void Tables_ContainsArTimeline()
	{
		SchemaManifest.Tables.Should().ContainKey("Ar_Timeline");
		SchemaManifest.Tables["Ar_Timeline"].RequiredColumns.Should()
			.Contain("ReportId")
			.And.Contain("SchemaName")
			.And.Contain("BaseSchemaName");
	}

	[TestMethod]
	public void Tables_ContainsArActivity()
	{
		SchemaManifest.Tables.Should().ContainKey("Ar_Activity");
		SchemaManifest.Tables["Ar_Activity"].RequiredColumns.Should()
			.Contain("ActivityId")
			.And.Contain("ReportId")
			.And.Contain("StartLocalTime")
			.And.Contain("EndLocalTime")
			.And.Contain("Name")
			.And.Contain("GroupId")
			.And.Contain("Notes")
			.And.Contain("IsActive")
			.And.Contain("CommonGroupId")
			.And.Contain("StartUtcTime")
			.And.Contain("EndUtcTime");
	}

	[TestMethod]
	public void Tables_ContainsArGroup()
	{
		SchemaManifest.Tables.Should().ContainKey("Ar_Group");
		SchemaManifest.Tables["Ar_Group"].RequiredColumns.Should()
			.Contain("GroupId")
			.And.Contain("ReportId")
			.And.Contain("Name")
			.And.Contain("Color")
			.And.Contain("Key")
			.And.Contain("CommonId");
	}

	[TestMethod]
	public void Tables_CaseInsensitiveLookup()
	{
		SchemaManifest.Tables.Should().ContainKey("ar_timeline");
		SchemaManifest.Tables.Should().ContainKey("AR_ACTIVITY");
	}

	[TestMethod]
	public void Tables_CoreTablesCount()
	{
		var coreTables = SchemaManifest.Tables.Values.Where(t => t.Tier == TableTier.Core).ToList();
		coreTables.Count.Should().Be(3);
	}

	[TestMethod]
	public void Tables_SupplementalTablesCount()
	{
		var supplementalTables = SchemaManifest.Tables.Values.Where(t => t.Tier == TableTier.Supplemental).ToList();
		supplementalTables.Count.Should().Be(13);
	}

	[TestMethod]
	public void Tables_InformationalTablesCount()
	{
		var informationalTables = SchemaManifest.Tables.Values.Where(t => t.Tier == TableTier.Informational).ToList();
		informationalTables.Count.Should().Be(2);
	}

	[TestMethod]
	public void Tables_ContainsPreAggregatedTables()
	{
		SchemaManifest.Tables.Should().ContainKey("Ar_ApplicationByDay");
		SchemaManifest.Tables.Should().ContainKey("Ar_WebSiteByDay");
		SchemaManifest.Tables.Should().ContainKey("Ar_DocumentByDay");
		SchemaManifest.Tables.Should().ContainKey("Ar_ApplicationByYear");
		SchemaManifest.Tables.Should().ContainKey("Ar_WebSiteByYear");
		SchemaManifest.Tables.Should().ContainKey("Ar_DocumentByYear");
		SchemaManifest.Tables.Should().ContainKey("Ar_ActivityByHour");
	}

	[TestMethod]
	public void Tables_ContainsTagTables()
	{
		SchemaManifest.Tables.Should().ContainKey("Ar_Tag");
		SchemaManifest.Tables.Should().ContainKey("Ar_ActivityTag");
	}

	[TestMethod]
	public void Tables_ContainsEnvironmentAndSummary()
	{
		SchemaManifest.Tables.Should().ContainKey("Ar_Environment");
		SchemaManifest.Tables.Should().ContainKey("Ar_TimelineSummary");
	}

	[TestMethod]
	public void Tables_AllTablesHaveTierAssigned()
	{
		foreach (var table in SchemaManifest.Tables.Values)
		{
			Enum.IsDefined(table.Tier).Should().BeTrue($"Table {table.TableName} should have a valid tier");
		}
	}
}
