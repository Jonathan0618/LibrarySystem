namespace LibrarySystem.Application.Catalog;

public interface ICatalogReferenceService
{
    Task<CatalogReferenceDto> AddAuthorAsync(string name, CancellationToken cancellationToken = default);

    Task<CatalogReferenceDto> AddCategoryAsync(string name, CancellationToken cancellationToken = default);

    Task<CatalogReferenceDto> AddPublisherAsync(string name, CancellationToken cancellationToken = default);

    Task<CatalogReferenceDto> AddShelfAsync(
        string code,
        string? description,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogReferenceDto>> GetAuthorsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogReferenceDto>> GetCategoriesAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogReferenceDto>> GetPublishersAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<CatalogReferenceDto>> GetShelvesAsync(CancellationToken cancellationToken = default);
}
