using System.ComponentModel.DataAnnotations;
using LibrarySystem.Domain.Catalog;

namespace LibrarySystem.Application.Circulation;

public sealed class ReturnLoanRequest
{
    [Required]
    public required byte[] RowVersion { get; init; }

    [EnumDataType(typeof(BookCondition))]
    public BookCondition Condition { get; init; } = BookCondition.Good;

    [MaxLength(500)]
    public string? Notes { get; init; }
}
