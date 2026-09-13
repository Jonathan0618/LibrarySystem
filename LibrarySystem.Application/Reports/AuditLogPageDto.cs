namespace LibrarySystem.Application.Reports;

public sealed class AuditLogPageDto
{
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public required IReadOnlyCollection<AuditLogDto> Items { get; init; }
}
