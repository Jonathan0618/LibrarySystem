using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Services.Identity;

public sealed class IdentityLinkOptions
{
    public const string SectionName = "Identity:Links";

    [Required, Url]
    public required string PublicBaseUrl { get; init; }

    [Range(5, 1440)]
    public int TokenLifetimeMinutes { get; init; } = 60;
}
