using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

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
        return new CatalogReferenceDto { Id = author.Id, Name = author.Name };
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
        return new CatalogReferenceDto { Id = category.Id, Name = category.Name };
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
        return new CatalogReferenceDto { Id = publisher.Id, Name = publisher.Name };
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
        return new CatalogReferenceDto { Id = shelf.Id, Name = shelf.Code };
    }

    public async Task<IReadOnlyCollection<CatalogReferenceDto>> GetAuthorsAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Authors.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new CatalogReferenceDto { Id = item.Id, Name = item.Name })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CatalogReferenceDto>> GetCategoriesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Categories.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new CatalogReferenceDto { Id = item.Id, Name = item.Name })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CatalogReferenceDto>> GetPublishersAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.Publishers.AsNoTracking().OrderBy(item => item.Name)
            .Select(item => new CatalogReferenceDto { Id = item.Id, Name = item.Name })
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyCollection<CatalogReferenceDto>> GetShelvesAsync(
        CancellationToken cancellationToken = default) =>
        await dbContext.ShelfLocations.AsNoTracking().OrderBy(item => item.Code)
            .Select(item => new CatalogReferenceDto { Id = item.Id, Name = item.Code })
            .ToListAsync(cancellationToken);

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
