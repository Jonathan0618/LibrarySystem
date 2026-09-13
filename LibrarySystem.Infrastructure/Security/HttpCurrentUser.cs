using System.Security.Claims;
using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Http;

namespace LibrarySystem.Infrastructure.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    public string? UserId => httpContextAccessor.HttpContext?.User
        .FindFirstValue(ClaimTypes.NameIdentifier);

    public bool IsAuthenticated => httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsInRole(string roleName) =>
        httpContextAccessor.HttpContext?.User.IsInRole(roleName) == true;
}
