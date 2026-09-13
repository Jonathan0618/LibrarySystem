namespace LibrarySystem.Application.Members;

public sealed class MemberRoleDto
{
    public required string UserId { get; init; }
    public required string Email { get; init; }
    public required string DisplayName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
    public bool IsActive { get; init; }
}
