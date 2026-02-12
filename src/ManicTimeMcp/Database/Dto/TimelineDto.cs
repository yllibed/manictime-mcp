namespace ManicTimeMcp.Database.Dto;

/// <summary>Read model for a ManicTime timeline row.</summary>
public sealed record TimelineDto
{
	/// <summary>Timeline identifier (Ar_Timeline.ReportId).</summary>
	public required long ReportId { get; init; }

	/// <summary>Schema name identifying the timeline type (e.g. ManicTime/Applications).</summary>
	public required string SchemaName { get; init; }

	/// <summary>Base schema name for timeline classification.</summary>
	public required string BaseSchemaName { get; init; }
}
