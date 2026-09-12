using System.ComponentModel.DataAnnotations;
using LibrarySystem.Domain.Members;

namespace LibrarySystem.Application.Members;

public sealed class MemberSearchRequest
{
    [MaxLength(100)]
    public string? SearchTerm { get; init; }

    public MemberType? MemberType { get; init; }

    public bool? IsActive { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
