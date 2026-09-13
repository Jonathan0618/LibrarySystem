using LibrarySystem.Domain.Members;
using LibrarySystem.Infrastructure.Circulation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace LibrarySystem.Infrastructure.Data;

public static class LibraryDataSeeder
{
    public static async Task SeedLibraryDataAsync(this IServiceProvider services)
    {
        await using var scope = services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var configuredTypes = await dbContext.LibraryPolicies
            .Select(policy => policy.MemberType)
            .ToListAsync();

        AddPolicyIfMissing(dbContext, configuredTypes, MemberType.Student, 14, 5, 1);
        AddPolicyIfMissing(dbContext, configuredTypes, MemberType.Teacher, 30, 10, 2);
        AddPolicyIfMissing(dbContext, configuredTypes, MemberType.Librarian, 30, 20, 3);
        await dbContext.SaveChangesAsync();
    }

    private static void AddPolicyIfMissing(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<MemberType> configuredTypes,
        MemberType memberType,
        int loanPeriodDays,
        int maximumActiveLoans,
        int maximumRenewals)
    {
        if (configuredTypes.Contains(memberType))
        {
            return;
        }

        dbContext.LibraryPolicies.Add(new LibraryPolicy
        {
            MemberType = memberType,
            LoanPeriodDays = loanPeriodDays,
            MaximumActiveLoans = maximumActiveLoans,
            MaximumRenewals = maximumRenewals,
            MaximumActiveReservations = memberType switch
            {
                MemberType.Student => 3,
                MemberType.Teacher => 5,
                MemberType.Librarian => 10,
                _ => 3
            }
        });
    }
}
