using LibrarySystem.Infrastructure.Catalog;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LibrarySystem.Infrastructure.Data.Configurations;

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("CatalogAuthors"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.HasIndex(x => x.Name).IsUnique();
    }
}

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("CatalogBooks"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Isbn).HasMaxLength(20);
        builder.Property(x => x.Title).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Edition).HasMaxLength(100);
        builder.Property(x => x.Description).HasMaxLength(4000);
        builder.Property(x => x.CoverImagePath).HasMaxLength(500);
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0);
        builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.Isbn).IsUnique();
        builder.HasIndex(x => x.Title); builder.HasIndex(x => x.IsArchived);
        builder.HasOne(x => x.Publisher).WithMany(x => x.Books)
            .HasForeignKey(x => x.PublisherId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        builder.ToTable("CatalogBookAuthors"); builder.HasKey(x => new { x.BookId, x.AuthorId });
        builder.HasOne(x => x.Book).WithMany(x => x.BookAuthors).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Author).WithMany(x => x.BookAuthors).HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BookCategoryConfiguration : IEntityTypeConfiguration<BookCategory>
{
    public void Configure(EntityTypeBuilder<BookCategory> builder)
    {
        builder.ToTable("CatalogBookCategories"); builder.HasKey(x => new { x.BookId, x.CategoryId });
        builder.HasOne(x => x.Book).WithMany(x => x.BookCategories).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Category).WithMany(x => x.BookCategories).HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class BookCopyConfiguration : IEntityTypeConfiguration<BookCopy>
{
    public void Configure(EntityTypeBuilder<BookCopy> builder)
    {
        builder.ToTable("CatalogBookCopies"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Barcode).HasMaxLength(100).IsRequired();
        builder.Property(x => x.AcquisitionPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CreatedAtUtc).HasPrecision(0); builder.Property(x => x.UpdatedAtUtc).HasPrecision(0);
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => x.Barcode).IsUnique();
        builder.HasIndex(x => new { x.BookId, x.Status }); builder.HasIndex(x => new { x.Status, x.BookId });
        builder.HasOne(x => x.Book).WithMany(x => x.Copies).HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ShelfLocation).WithMany(x => x.BookCopies).HasForeignKey(x => x.ShelfLocationId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("CatalogCategories"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired(); builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
    }
}

public sealed class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.ToTable("CatalogPublishers"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired(); builder.HasIndex(x => x.Name).IsUnique();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
    }
}

public sealed class ShelfLocationConfiguration : IEntityTypeConfiguration<ShelfLocation>
{
    public void Configure(EntityTypeBuilder<ShelfLocation> builder)
    {
        builder.ToTable("CatalogShelfLocations"); builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(200); builder.HasIndex(x => x.Code).IsUnique();
        builder.Property(x => x.IsActive).HasDefaultValue(true);
    }
}
