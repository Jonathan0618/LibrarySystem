using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.Reports;

public sealed class AuditLogRequest
{
    [StringLength(200)]
    public string? SearchTerm { get; init; }

    [DataType(DataType.Date)]
    public DateTime? FromDate { get; init; }

    [DataType(DataType.Date)]
    public DateTime? ToDate { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 200)]
    public int PageSize { get; init; } = 50;
}
