namespace LibrarySystem.Application.Members;

public interface IRoleManagementService
{
    Task<IReadOnlyList<MemberRoleDto>> ListAsync(CancellationToken cancellationToken = default);
    Task SetRoleAsync(string userId, string roleName, bool assigned, CancellationToken cancellationToken = default);
}
