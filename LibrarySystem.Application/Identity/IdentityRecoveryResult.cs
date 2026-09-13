namespace LibrarySystem.Application.Identity;

public sealed record IdentityRecoveryResult(bool Succeeded, IReadOnlyCollection<string> Errors)
{
    public static IdentityRecoveryResult Success { get; } = new(true, []);

    public static IdentityRecoveryResult Failure(IEnumerable<string> errors) => new(false, errors.ToArray());
}
