namespace LibrarySystem.Application.Catalog;

public sealed class CatalogReferenceDto
{
    public int Id { get; init; }

    public required string Name { get; init; }

    public string? Description { get; init; }

    public bool IsActive { get; init; }

    public int UsageCount { get; init; }
}
