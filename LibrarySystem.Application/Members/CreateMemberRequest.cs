using System.ComponentModel.DataAnnotations;
using LibrarySystem.Domain.Members;

namespace LibrarySystem.Application.Members;

public sealed class CreateMemberRequest
{
    [Required]
    [EmailAddress]
    [MaxLength(256)]
    public required string Email { get; init; }

    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }

    [Required]
    [MinLength(12)]
    [MaxLength(100)]
    public required string TemporaryPassword { get; init; }

    [EnumDataType(typeof(MemberType))]
    public MemberType MemberType { get; init; }

    [MaxLength(50)]
    public string? Grade { get; init; }

    [MaxLength(100)]
    public string? Department { get; init; }
}
