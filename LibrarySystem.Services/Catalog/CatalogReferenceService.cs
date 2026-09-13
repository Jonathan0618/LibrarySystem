using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Infrastructure.Catalog;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Catalog;

public sealed class CatalogReferenceService(ApplicationDbContext dbContext) : ICatalogReferenceService
{
    public async Task<CatalogReferenceDto> AddAuthorAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeRequired(name, 200, nameof(name));
        if (await dbContext.Authors.AnyAsync(item => item.Name == normalizedName, cancellationToken))
        {
            throw new ValidationException("An author with this name already exists.");
        }

        var author = new Author { Name = normalizedName };
        dbContext.Authors.Add(author);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CatalogReferenceDto { Id = author.Id, Name = author.Name, IsActive = true };
    }

    public async Task<CatalogReferenceDto> AddCategoryAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeRequired(name, 100, nameof(name));
        if (await dbContext.Categories.AnyAsync(item => item.Name == normalizedName, cancellationToken))
        {
            throw new ValidationException("A category with this name already exists.");
        }

        var category = new Category { Name = normalizedName };
        dbContext.Categories.Add(category);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CatalogReferenceDto { Id = category.Id, Name = category.Name, IsActive = true };
    }

    public async Task<CatalogReferenceDto> AddPublisherAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var normalizedName = NormalizeRequired(name, 200, nameof(name));
        if (await dbContext.Publishers.AnyAsync(item => item.Name == normalizedName, cancellationToken))
        {
            throw new ValidationException("A publisher with this name already exists.");
        }

        var publisher = new Publisher { Name = normalizedName };
        dbContext.Publishers.Add(publisher);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CatalogReferenceDto { Id = publisher.Id, Name = publisher.Name, IsActive = true };
    }

    public async Task<CatalogReferenceDto> AddShelfAsync(
        string code,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = NormalizeRequired(code, 50, nameof(code)).ToUpperInvariant();
        if (await dbContext.ShelfLocations.AnyAsync(item => item.Code == normalizedCode, cancellationToken))
        {
            throw new ValidationException("A shelf with this code already exists.");
        }

        var shelf = new ShelfLocation
        {
            Code = normalizedCode,
            Description = NormalizeOptional(description, 200, nameof(description))
        };
        dbContext.ShelfLocations.Add(shelf);
        await dbContext.SaveChangesAsync(cancellationToken);
        return new CatalogReferenceDto { Id = shelf.Id, Name = shelf.Code, Description = shelf.Description, IsActive = true };
    }

    public async Task<IReadOnlyCollection<CatalogReferenceDto>> GetAuthorsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Authors.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new CatalogReferenceDto { Id = item.Id, Name = item.Name, IsActive = item.IsActive, UsageCount = item.BookAuthors.Count })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CatalogReferenceDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Categories.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new CatalogReferenceDto { Id = item.Id, Name = item.Name, IsActive = item.IsActive, UsageCount = item.BookCategories.Count })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CatalogReferenceDto>> GetPublishersAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Publishers.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new CatalogReferenceDto { Id = item.Id, Name = item.Name, IsActive = item.IsActive, UsageCount = item.Books.Count })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CatalogReferenceDto>> GetShelvesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.ShelfLocations.AsNoTracking().OrderBy(item => item.Code)
            .Select(item => new CatalogReferenceDto { Id = item.Id, Name = item.Code, Description = item.Description, IsActive = item.IsActive, UsageCount = item.BookCopies.Count })
            .ToListAsync(cancellationToken);

    public async Task<CatalogReferenceDto?> UpdateAsync(
        string type,
        int id,
        string name,
        string? description,
        CancellationToken cancellationToken = default)
    {
        var normalizedType = NormalizeType(type);
        var maximumLength = normalizedType == "category" ? 100 : normalizedType == "shelf" ? 50 : 200;
        var normalizedName = NormalizeRequired(name, maximumLength, nameof(name));
        if (normalizedType == "shelf")
        {
            normalizedName = normalizedName.ToUpperInvariant();
        }

        var duplicate = normalizedType switch
        {
            "author" => await dbContext.Authors.AnyAsync(item => item.Name == normalizedName && item.Id != id, cancellationToken),
            "category" => await dbContext.Categories.AnyAsync(item => item.Name == normalizedName && item.Id != id, cancellationToken),
            "publisher" => await dbContext.Publishers.AnyAsync(item => item.Name == normalizedName && item.Id != id, cancellationToken),
            _ => await dbContext.ShelfLocations.AnyAsync(item => item.Code == normalizedName && item.Id != id, cancellationToken)
        };
        if (duplicate)
        {
            throw new ValidationException($"A {normalizedType} with this name already exists.");
        }

        CatalogReferenceDto? result;
        switch (normalizedType)
        {
            case "author":
                var author = await dbContext.Authors.FindAsync([id], cancellationToken);
                if (author is null) return null;
                author.Name = normalizedName;
                result = new CatalogReferenceDto { Id = id, Name = author.Name, IsActive = author.IsActive, UsageCount = author.BookAuthors.Count };
                break;
            case "category":
                var category = await dbContext.Categories.FindAsync([id], cancellationToken);
                if (category is null) return null;
                category.Name = normalizedName;
                result = new CatalogReferenceDto { Id = id, Name = category.Name, IsActive = category.IsActive, UsageCount = category.BookCategories.Count };
                break;
            case "publisher":
                var publisher = await dbContext.Publishers.FindAsync([id], cancellationToken);
                if (publisher is null) return null;
                publisher.Name = normalizedName;
                result = new CatalogReferenceDto { Id = id, Name = publisher.Name, IsActive = publisher.IsActive, UsageCount = publisher.Books.Count };
                break;
            default:
                var shelf = await dbContext.ShelfLocations.FindAsync([id], cancellationToken);
                if (shelf is null) return null;
                shelf.Code = normalizedName;
                shelf.Description = NormalizeOptional(description, 200, nameof(description));
                result = new CatalogReferenceDto { Id = id, Name = shelf.Code, Description = shelf.Description, IsActive = shelf.IsActive, UsageCount = shelf.BookCopies.Count };
                break;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return result;
    }

    public async Task<bool> SetActiveAsync(
        string type,
        int id,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        switch (NormalizeType(type))
        {
            case "author":
                var author = await dbContext.Authors.FindAsync([id], cancellationToken);
                if (author is null) return false;
                author.IsActive = isActive;
                break;
            case "category":
                var category = await dbContext.Categories.FindAsync([id], cancellationToken);
                if (category is null) return false;
                category.IsActive = isActive;
                break;
            case "publisher":
                var publisher = await dbContext.Publishers.FindAsync([id], cancellationToken);
                if (publisher is null) return false;
                publisher.IsActive = isActive;
                break;
            default:
                var shelf = await dbContext.ShelfLocations.FindAsync([id], cancellationToken);
                if (shelf is null) return false;
                shelf.IsActive = isActive;
                break;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizeType(string type) => type.Trim().ToLowerInvariant() switch
    {
        "author" => "author",
        "category" => "category",
        "publisher" => "publisher",
        "shelf" => "shelf",
        _ => throw new ValidationException("The catalog reference type is invalid.")
    };

    private static string NormalizeRequired(string value, int maxLength, string propertyName)
    {
        var normalized = value.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
        {
            throw new ValidationException($"{propertyName} is required and cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ValidationException($"{propertyName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
