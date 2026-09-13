namespace LibrarySystem.Application.Security;

public interface ICurrentUser
{
    string? UserId { get; }

    bool IsAuthenticated { get; }

    bool IsInRole(string roleName);
}
