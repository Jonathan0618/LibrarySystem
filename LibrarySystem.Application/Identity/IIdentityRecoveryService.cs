namespace LibrarySystem.Application.Identity;

public interface IIdentityRecoveryService
{
    Task QueueEmailConfirmationAsync(
        string userId,
        bool renewToken = false,
        CancellationToken cancellationToken = default);

    Task QueuePasswordResetAsync(string email, CancellationToken cancellationToken = default);

    Task<IdentityRecoveryResult> QueueEmailChangeAsync(
        string userId,
        string newEmail,
        CancellationToken cancellationToken = default);

    Task<IdentityRecoveryResult> ConfirmEmailAsync(
        string userId,
        string encodedToken,
        CancellationToken cancellationToken = default);

    Task<IdentityRecoveryResult> ResetPasswordAsync(
        string userId,
        string encodedToken,
        string newPassword,
        CancellationToken cancellationToken = default);

    Task<IdentityRecoveryResult> ConfirmEmailChangeAsync(
        string userId,
        string newEmail,
        string encodedToken,
        CancellationToken cancellationToken = default);
}
