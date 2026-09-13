using System.ComponentModel.DataAnnotations;
using LibrarySystem.Domain.Circulation;
using LibrarySystem.Domain.Members;

namespace LibrarySystem.Application.Reports;

public sealed class CirculationReportRequest
{
    [DataType(DataType.Date)]
    public DateTime? FromDate { get; init; }
    [DataType(DataType.Date)]
    public DateTime? ToDate { get; init; }
    public LoanStatus? Status { get; init; }
    public MemberType? MemberType { get; init; }
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; init; }
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;
    [Range(1, 200)]
    public int PageSize { get; init; } = 50;
}
