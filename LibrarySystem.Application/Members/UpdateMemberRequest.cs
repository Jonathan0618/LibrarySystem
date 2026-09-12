using System.ComponentModel.DataAnnotations;
using LibrarySystem.Domain.Members;

namespace LibrarySystem.Application.Members;

public sealed class UpdateMemberRequest
{
    [Required]
    [MaxLength(100)]
    public required string FirstName { get; init; }

    [Required]
    [MaxLength(100)]
    public required string LastName { get; init; }

    [EnumDataType(typeof(MemberType))]
    public MemberType MemberType { get; init; }

    [MaxLength(50)]
    public string? Grade { get; init; }

    [MaxLength(100)]
    public string? Department { get; init; }
}
