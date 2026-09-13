using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.Fines;

public sealed class FineActionRequest
{
    [Range(typeof(decimal), "0.01", "100000")] public decimal Amount { get; init; }
    [Required, MaxLength(500)] public required string Reason { get; init; }
    [Required] public required byte[] RowVersion { get; init; }
}
