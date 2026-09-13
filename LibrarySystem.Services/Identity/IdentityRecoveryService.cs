using System.Security.Cryptography;
using System.Text;
using LibrarySystem.Application.Identity;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Notifications;
using LibrarySystem.Infrastructure.Data;
using LibrarySystem.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace LibrarySystem.Services.Identity;

public sealed class IdentityRecoveryService(
    UserManager<ApplicationUser> userManager,
    IAccountNotificationService notificationService,
    ApplicationDbContext dbContext,
    IOptions<IdentityLinkOptions> options) : IIdentityRecoveryService
{
    private readonly string _publicBaseUrl = options.Value.PublicBaseUrl.TrimEnd('/');

    public async Task QueueEmailConfirmationAsync(
        string userId,
        bool renewToken = false,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive || user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email)) return;

        if (renewToken)
        {
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                throw new InvalidOperationException("Unable to renew the email-confirmation token.");
            }
        }

        var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
        var encodedToken = EncodeToken(token);
        var link = BuildLink("/Identity/Account/ConfirmEmail", user.Id, encodedToken);
        await notificationService.QueueAsync(
            $"confirm-email:{user.Id}:{TokenFingerprint(encodedToken)}",
            user.Email,
            "Confirm your school library account",
            $"Confirm your school library account using this link: {link}",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task QueuePasswordResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim();
        var user = await userManager.FindByEmailAsync(normalizedEmail);
        if (user is null || !user.IsActive || !user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email)) return;

        var token = await userManager.GeneratePasswordResetTokenAsync(user);
        var encodedToken = EncodeToken(token);
        var link = BuildLink("/Identity/Account/ResetPassword", user.Id, encodedToken);
        await notificationService.QueueAsync(
            $"reset-password:{user.Id}:{TokenFingerprint(encodedToken)}",
            user.Email,
            "Reset your school library password",
            $"Reset your school library password using this link: {link}",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IdentityRecoveryResult> QueueEmailChangeAsync(
        string userId,
        string newEmail,
        CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        var normalizedEmail = newEmail.Trim();
        if (user is null || !user.IsActive || !user.EmailConfirmed) return InvalidToken();
        if (!SchoolEmailAddressAttribute.IsSchoolEmail(normalizedEmail))
        {
            return IdentityRecoveryResult.Failure(["Use your @nvsu.edu.ph school email address."]);
        }
        if (string.Equals(user.Email, normalizedEmail, StringComparison.OrdinalIgnoreCase))
        {
            return IdentityRecoveryResult.Failure(["Enter a different email address."]);
        }
        if (await userManager.FindByEmailAsync(normalizedEmail) is not null)
        {
            return IdentityRecoveryResult.Failure(["That email address is already in use."]);
        }

        var token = await userManager.GenerateChangeEmailTokenAsync(user, normalizedEmail);
        var encodedToken = EncodeToken(token);
        var link = QueryHelpers.AddQueryString(
            $"{_publicBaseUrl}/Identity/Account/ConfirmEmailChange",
            new Dictionary<string, string?>
            {
                ["userId"] = user.Id,
                ["newEmail"] = normalizedEmail,
                ["code"] = encodedToken
            });
        await notificationService.QueueAsync(
            $"change-email:{user.Id}:{TokenFingerprint(encodedToken)}",
            normalizedEmail,
            "Confirm your new school library email",
            $"Confirm your new email address using this link: {link}",
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return IdentityRecoveryResult.Success;
    }

    public async Task<IdentityRecoveryResult> ConfirmEmailAsync(
        string userId,
        string encodedToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive) return InvalidToken();
        var token = DecodeToken(encodedToken);
        if (token is null) return InvalidToken();

        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await userManager.ConfirmEmailAsync(user, token);
        return Map(result);
    }

    public async Task<IdentityRecoveryResult> ResetPasswordAsync(
        string userId,
        string encodedToken,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId);
        if (user is null || !user.IsActive || !user.EmailConfirmed) return InvalidToken();
        var token = DecodeToken(encodedToken);
        if (token is null) return InvalidToken();
        user.MustChangePassword = false;
        user.UpdatedAtUtc = DateTime.UtcNow;
        var result = await userManager.ResetPasswordAsync(user, token, newPassword);
        return Map(result);
    }

    public async Task<IdentityRecoveryResult> ConfirmEmailChangeAsync(
        string userId,
        string newEmail,
        string encodedToken,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var user = await userManager.FindByIdAsync(userId);
        var normalizedEmail = newEmail.Trim();
        var token = DecodeToken(encodedToken);
        if (user is null || !user.IsActive || token is null) return InvalidToken();

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
        user.UpdatedAtUtc = DateTime.UtcNow;
        var emailResult = await userManager.ChangeEmailAsync(user, normalizedEmail, token);
        if (!emailResult.Succeeded) return Map(emailResult);
        var userNameResult = await userManager.SetUserNameAsync(user, normalizedEmail);
        if (!userNameResult.Succeeded) return Map(userNameResult);
        await transaction.CommitAsync(cancellationToken);
        return IdentityRecoveryResult.Success;
    }

    private string BuildLink(string path, string userId, string token) =>
        QueryHelpers.AddQueryString($"{_publicBaseUrl}{path}", new Dictionary<string, string?>
        {
            ["userId"] = userId,
            ["code"] = token
        });

    private static string EncodeToken(string token) =>
        WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    private static string? DecodeToken(string encodedToken)
    {
        try { return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken)); }
        catch (FormatException) { return null; }
    }

    private static string TokenFingerprint(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)))[..16];

    private static IdentityRecoveryResult Map(IdentityResult result) => result.Succeeded
        ? IdentityRecoveryResult.Success
        : IdentityRecoveryResult.Failure(result.Errors.Select(error => error.Description));

    private static IdentityRecoveryResult InvalidToken() =>
        IdentityRecoveryResult.Failure(["The recovery link is invalid or has expired."]);
}
