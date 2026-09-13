using System.ComponentModel.DataAnnotations;
using LibrarySystem.Domain.Circulation;

namespace LibrarySystem.Application.Circulation;

public sealed class LoanSearchRequest
{
    [MaxLength(200)]
    public string? SearchTerm { get; init; }
    public LoanStatus? Status { get; init; }
    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;
    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
