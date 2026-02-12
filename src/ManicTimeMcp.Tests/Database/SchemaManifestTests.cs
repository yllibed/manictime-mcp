using AwesomeAssertions;
using ManicTimeMcp.Database;

namespace ManicTimeMcp.Tests.Database;

[TestClass]
public sealed class SchemaManifestTests
{
	[TestMethod]
	public void Tables_ContainsExpectedTableCount()
	{
		SchemaManifest.Tables.Count.Should().Be(3);
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
			.And.Contain("GroupId");
	}

	[TestMethod]
	public void Tables_ContainsArGroup()
	{
		SchemaManifest.Tables.Should().ContainKey("Ar_Group");
		SchemaManifest.Tables["Ar_Group"].RequiredColumns.Should()
			.Contain("GroupId")
			.And.Contain("ReportId")
			.And.Contain("Name");
	}

	[TestMethod]
	public void Tables_CaseInsensitiveLookup()
	{
		SchemaManifest.Tables.Should().ContainKey("ar_timeline");
		SchemaManifest.Tables.Should().ContainKey("AR_ACTIVITY");
	}
}
