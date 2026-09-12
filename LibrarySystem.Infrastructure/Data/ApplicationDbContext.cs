using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Catalog;
using LibrarySystem.Infrastructure.Identity;
using LibrarySystem.Infrastructure.Members;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Data;

public sealed class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser, ApplicationRole, string>(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    public DbSet<Author> Authors => Set<Author>();

    public DbSet<Book> Books => Set<Book>();

    public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();

    public DbSet<BookCategory> BookCategories => Set<BookCategory>();

    public DbSet<BookCopy> BookCopies => Set<BookCopy>();

    public DbSet<Category> Categories => Set<Category>();

    public DbSet<LibraryMember> LibraryMembers => Set<LibraryMember>();

    public DbSet<Publisher> Publishers => Set<Publisher>();

    public DbSet<ShelfLocation> ShelfLocations => Set<ShelfLocation>();
}
