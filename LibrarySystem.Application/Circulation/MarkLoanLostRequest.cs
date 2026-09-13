using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.Circulation;

public sealed class MarkLoanLostRequest
{
    [Required]
    public required byte[] RowVersion { get; init; }

    [MaxLength(500)]
    public string? Notes { get; init; }
}
