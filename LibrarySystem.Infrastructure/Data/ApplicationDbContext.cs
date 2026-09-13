using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Catalog;
using LibrarySystem.Infrastructure.Circulation;
using LibrarySystem.Infrastructure.Identity;
using LibrarySystem.Infrastructure.Fines;
using LibrarySystem.Infrastructure.Members;
using LibrarySystem.Infrastructure.Notifications;
using LibrarySystem.Infrastructure.Reservations;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();

    public DbSet<BookCategory> BookCategories => Set<BookCategory>();

    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<Loan> Loans => Set<Loan>();

    public DbSet<LoanHistory> LoanHistory => Set<LoanHistory>();

    public DbSet<LibraryMember> LibraryMembers => Set<LibraryMember>();

    public DbSet<LibraryPolicy> LibraryPolicies => Set<LibraryPolicy>();

    public DbSet<Fine> Fines => Set<Fine>();

    public DbSet<FineTransaction> FineTransactions => Set<FineTransaction>();

    public DbSet<QueuedNotification> QueuedNotifications => Set<QueuedNotification>();

    public DbSet<Publisher> Publishers => Set<Publisher>();

    public DbSet<Reservation> Reservations => Set<Reservation>();

    public DbSet<SchoolHoliday> SchoolHolidays => Set<SchoolHoliday>();

    public DbSet<ShelfLocation> ShelfLocations => Set<ShelfLocation>();
    public DbSet<InventorySession> InventorySessions => Set<InventorySession>();
    public DbSet<InventoryExpectedCopy> InventoryExpectedCopies => Set<InventoryExpectedCopy>();
    public DbSet<InventoryScan> InventoryScans => Set<InventoryScan>();
    public DbSet<CopyWithdrawal> CopyWithdrawals => Set<CopyWithdrawal>();
    public DbSet<AcquisitionBudget> AcquisitionBudgets => Set<AcquisitionBudget>();
    public DbSet<AcquisitionRecord> AcquisitionRecords => Set<AcquisitionRecord>();
    public DbSet<AcquisitionReceipt> AcquisitionReceipts => Set<AcquisitionReceipt>();
}
