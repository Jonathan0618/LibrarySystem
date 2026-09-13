using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.Circulation;

public sealed class RenewLoanRequest
{
    [Required]
    public required byte[] RowVersion { get; init; }
}
