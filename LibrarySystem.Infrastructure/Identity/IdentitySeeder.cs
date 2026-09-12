using LibrarySystem.Application.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LibrarySystem.Infrastructure.Identity;

public static partial class IdentitySeeder
{
    public static async Task SeedIdentityAsync(
        this IServiceProvider services,
        IConfiguration configuration)
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
            await SeedTemporaryLibrarianAsync(userManager, configuration);
            return;
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                "Both IdentitySeed:AdministratorEmail and IdentitySeed:AdministratorPassword must be configured.");
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
                LastName = "Administrator"
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

        await SeedTemporaryLibrarianAsync(userManager, configuration);
    }

    private static async Task SeedTemporaryLibrarianAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration)
    {
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
}
