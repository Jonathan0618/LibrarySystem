using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.Catalog;

public sealed class CatalogSearchRequest
{
    [MaxLength(200)]
    public string? SearchTerm { get; init; }

    public int? AuthorId { get; init; }

    public int? CategoryId { get; init; }

    public bool IncludeArchived { get; init; }

    [Range(1, int.MaxValue)]
    public int PageNumber { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
