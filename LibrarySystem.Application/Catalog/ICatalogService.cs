using LibrarySystem.Application.Common;

namespace LibrarySystem.Application.Catalog;

public interface ICatalogService
{
    Task<BookDto> CreateBookAsync(
        CreateBookRequest request,
        CancellationToken cancellationToken = default);

    Task<BookDto?> GetBookAsync(
        long id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<BookDto>> SearchAsync(
        CatalogSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<BookDto?> UpdateBookAsync(
        long id,
        UpdateBookRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> SetBookArchivedAsync(
        long id,
        bool isArchived,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<string?> SetCoverAsync(
        long id,
        Stream content,
        string fileName,
        string contentType,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<BookCopyDto> AddCopyAsync(
        long bookId,
        CreateBookCopyRequest request,
        CancellationToken cancellationToken = default);

    Task<BookCopyDto?> GetCopyAsync(long copyId, CancellationToken cancellationToken = default);

    Task<BookCopyDto?> UpdateCopyAsync(
        long copyId,
        UpdateBookCopyRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> SetCopyStatusAsync(
        long copyId,
        LibrarySystem.Domain.Catalog.BookCopyStatus status,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);

    Task<bool> WithdrawCopyAsync(
        long copyId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default);
}
