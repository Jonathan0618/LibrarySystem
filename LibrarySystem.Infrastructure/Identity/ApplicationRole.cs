using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace LibrarySystem.Infrastructure.Identity;

public sealed class ApplicationRole : IdentityRole
{
    [MaxLength(250)]
    public string? Description { get; set; }
}
