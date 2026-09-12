using System.ComponentModel.DataAnnotations;

namespace LibrarySystem.Application.Catalog;

public sealed class CreateBookRequest
{
    [MaxLength(20)]
    public string? Isbn { get; init; }

    [Required]
    [MaxLength(300)]
    public required string Title { get; init; }

    [MaxLength(100)]
    public string? Edition { get; init; }

    [Range(1000, 9999)]
    public int? PublicationYear { get; init; }

    [MaxLength(4000)]
    public string? Description { get; init; }

    public int? PublisherId { get; init; }

    public IReadOnlyCollection<int> AuthorIds { get; init; } = [];

    public IReadOnlyCollection<int> CategoryIds { get; init; } = [];
}
