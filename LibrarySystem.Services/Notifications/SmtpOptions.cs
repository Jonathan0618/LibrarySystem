using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Services.Notifications;

public sealed class SmtpOptions
{
    public const string SectionName = "Notifications:Smtp";

    [Required]
    public required string Host { get; init; }

    [Range(1, 65535)]
    public int Port { get; init; } = 587;

    [Required]
    public required string UserName { get; init; }

    [Required]
    public required string Password { get; init; }

    [Required, EmailAddress]
    public required string FromAddress { get; init; }

    [Required]
    public required string FromName { get; init; }

    public bool EnableSsl { get; init; } = true;
}
