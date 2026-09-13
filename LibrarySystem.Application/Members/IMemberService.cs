using LibrarySystem.Application.Common;

namespace LibrarySystem.Application.Members;

public interface IMemberService
{
    Task<MemberDto> CreateAsync(
        CreateMemberRequest request,
        CancellationToken cancellationToken = default);

    Task<MemberDto?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<MemberDetailsDto?> GetDetailsAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<MemberDto>> SearchAsync(
        MemberSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<MemberDto?> UpdateAsync(
        long id,
        UpdateMemberRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> SetActiveStatusAsync(
        long id,
        bool isActive,
        CancellationToken cancellationToken = default);
}
