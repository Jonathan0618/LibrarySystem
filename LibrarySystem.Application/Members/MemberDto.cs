using LibrarySystem.Domain.Members;

namespace LibrarySystem.Application.Members;

public sealed class MemberDto
{
    public long Id { get; init; }

    public required string UserId { get; init; }

    public required string Email { get; init; }

    public required string MemberNumber { get; init; }

    public required string FirstName { get; init; }

    public required string LastName { get; init; }

    public MemberType MemberType { get; init; }

    public string? Grade { get; init; }

    public string? Department { get; init; }

    public bool IsActive { get; init; }

    public bool EmailConfirmed { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}
