using System.ComponentModel.DataAnnotations;
using System.Net.Mail;

namespace LibrarySystem.Application.Common;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
public sealed class SchoolEmailAddressAttribute : ValidationAttribute
{
    public const string RequiredDomain = "nvsu.edu.ph";

    public SchoolEmailAddressAttribute()
        : base("Use your @nvsu.edu.ph school email address.")
    {
    }

    public override bool IsValid(object? value) =>
        value is null || value is string text && IsSchoolEmail(text);

    public static bool IsSchoolEmail(string email) =>
        MailAddress.TryCreate(email.Trim(), out var address) &&
        string.Equals(address.Address, email.Trim(), StringComparison.OrdinalIgnoreCase) &&
        string.Equals(address.Host, RequiredDomain, StringComparison.OrdinalIgnoreCase);
}
