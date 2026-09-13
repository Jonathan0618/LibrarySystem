using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.Circulation;

public sealed class CheckoutRequest
{
    public Guid OperationId { get; init; }

    [Range(1, long.MaxValue)]
    public long MemberId { get; init; }

    [Required]
    [MinLength(1)]
    [MaxLength(25)]
    public required IReadOnlyCollection<string> Barcodes { get; init; }
}
