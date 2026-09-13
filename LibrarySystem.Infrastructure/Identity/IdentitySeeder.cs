using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Infrastructure.Identity;

public static partial class IdentitySeeder
{
    public static async Task SeedIdentityAsync(
        this IServiceProvider services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        await using var scope = services.CreateAsyncScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var logger = scope.ServiceProvider
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger(typeof(IdentitySeeder));

        foreach (var roleName in RoleNames.All)
        {
            if (await roleManager.RoleExistsAsync(roleName))
            {
                continue;
            }

            var roleResult = await roleManager.CreateAsync(new ApplicationRole
            {
                Name = roleName
            });

            EnsureSucceeded(roleResult, $"create the {roleName} role");
        }

        var email = configuration["IdentitySeed:AdministratorEmail"];
        var password = configuration["IdentitySeed:AdministratorPassword"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            LogAdministratorSeedSkipped(logger);
            await SeedTemporaryLibrarianAsync(userManager, configuration, environment);
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both IdentitySeed:AdministratorEmail and IdentitySeed:AdministratorPassword must be configured.");
        }

        if (!configuration.GetValue<bool>("IdentitySeed:EnableAdministratorBootstrap"))
        {
            throw new InvalidOperationException(
                "Administrator seed credentials are configured, but IdentitySeed:EnableAdministratorBootstrap is not enabled.");
        }

        var existingAdministrators = await userManager.GetUsersInRoleAsync(RoleNames.Administrator);
        if (existingAdministrators.Count > 0 &&
            !existingAdministrators.Any(user =>
                string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException(
                "An administrator already exists. Refusing to bootstrap a different administrator account.");
        }

        var administrator = await userManager.FindByEmailAsync(email);
        if (administrator is null)
        {
            administrator = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "System",
                LastName = "Administrator",
                MustChangePassword = true
            };

            var userResult = await userManager.CreateAsync(administrator, password);
            EnsureSucceeded(userResult, "create the initial administrator");
        }

        if (!await userManager.IsInRoleAsync(administrator, RoleNames.Administrator))
        {
            var assignmentResult = await userManager.AddToRoleAsync(
                administrator,
                RoleNames.Administrator);
            EnsureSucceeded(assignmentResult, "assign the Administrator role");
        }

        LogAdministratorBootstrapEnabled(logger, administrator.Id);

        await SeedTemporaryLibrarianAsync(userManager, configuration, environment);
    }

    private static async Task SeedTemporaryLibrarianAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var enabled = configuration.GetValue<bool>("IdentitySeed:EnableTemporaryLibrarian");
        if (!enabled)
        {
            return;
        }

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Temporary librarian seeding can only be enabled in Development.");
        }

        var email = configuration["IdentitySeed:TemporaryLibrarianEmail"];
        var password = configuration["IdentitySeed:TemporaryLibrarianPassword"];

        if (string.IsNullOrWhiteSpace(email) && string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both temporary librarian seed credentials must be configured.");
        }

        var librarian = await userManager.FindByEmailAsync(email);
        if (librarian is null)
        {
            librarian = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                FirstName = "Temporary",
                LastName = "Librarian"
            };

            EnsureSucceeded(
                await userManager.CreateAsync(librarian, password),
                "create the temporary librarian");
        }

        if (!await userManager.IsInRoleAsync(librarian, RoleNames.Librarian))
        {
            EnsureSucceeded(
                await userManager.AddToRoleAsync(librarian, RoleNames.Librarian),
                "assign the Librarian role");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(
            "; ",
            result.Errors.Select(error => $"{error.Code}: {error.Description}"));
        throw new InvalidOperationException($"Unable to {operation}. {errors}");
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Initial administrator creation was skipped because no seed credentials were configured.")]
    private static partial void LogAdministratorSeedSkipped(ILogger logger);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Administrator bootstrap is enabled for user {UserId}. Remove the bootstrap switch and credentials immediately after verification.")]
    private static partial void LogAdministratorBootstrapEnabled(ILogger logger, string userId);
}
