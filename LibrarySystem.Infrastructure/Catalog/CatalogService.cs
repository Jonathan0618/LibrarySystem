using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Catalog;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Infrastructure.Catalog;

public sealed class CatalogService(
    ApplicationDbContext dbContext,
    ICurrentUser currentUser,
    IBookCoverStorage coverStorage) : ICatalogService
{
    public async Task<BookDto> CreateBookAsync(
        CreateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var title = NormalizeRequired(request.Title, 300, nameof(request.Title));
        var isbn = NormalizeOptional(request.Isbn, 20, nameof(request.Isbn));
        await ValidateIsbnAvailableAsync(isbn, null, cancellationToken);

        var authors = await LoadAuthorsAsync(request.AuthorIds, cancellationToken);
        var categories = await LoadCategoriesAsync(request.CategoryIds, cancellationToken);
        await ValidatePublisherAsync(request.PublisherId, cancellationToken);

        var book = new Book
        {
            Title = title,
            Isbn = isbn,
            Edition = NormalizeOptional(request.Edition, 100, nameof(request.Edition)),
            PublicationYear = request.PublicationYear,
            Description = NormalizeOptional(request.Description, 4000, nameof(request.Description)),
            PublisherId = request.PublisherId
        };

        book.BookAuthors = authors
            .Select(author => new BookAuthor { Book = book, Author = author, AuthorId = author.Id })
            .ToList();
        book.BookCategories = categories
            .Select(category => new BookCategory { Book = book, Category = category, CategoryId = category.Id })
            .ToList();

        dbContext.Books.Add(book);
        await dbContext.SaveChangesAsync(cancellationToken);
        AddAudit("BookCreated", nameof(Book), book.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return await GetRequiredBookAsync(book.Id, cancellationToken);
    }

    public async Task<BookDto?> GetBookAsync(
        long id,
        CancellationToken cancellationToken = default)
    {
        var book = await CompleteBookQuery()
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        return book is null ? null : MapBook(book);
    }

    public async Task<PagedResult<BookDto>> SearchAsync(
        CatalogSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var pageNumber = Math.Max(request.PageNumber, 1);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var query = dbContext.Books.AsNoTracking();

        if (!request.IncludeArchived)
        {
            query = query.Where(book => !book.IsArchived);
        }

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim();
            query = query.Where(book =>
                book.Title.Contains(term) ||
                (book.Isbn != null && book.Isbn.Contains(term)) ||
                book.BookAuthors.Any(item => item.Author.Name.Contains(term)) ||
                book.Copies.Any(copy => copy.Barcode.Contains(term)));
        }

        if (request.AuthorId.HasValue)
        {
            query = query.Where(book =>
                book.BookAuthors.Any(item => item.AuthorId == request.AuthorId.Value));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(book =>
                book.BookCategories.Any(item => item.CategoryId == request.CategoryId.Value));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var books = await query
            .Include(book => book.Publisher)
            .Include(book => book.BookAuthors).ThenInclude(item => item.Author)
            .Include(book => book.BookCategories).ThenInclude(item => item.Category)
            .Include(book => book.Copies)
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .AsSplitQuery()
            .ToListAsync(cancellationToken);

        return new PagedResult<BookDto>
        {
            Items = books.Select(MapBook).ToArray(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<BookDto?> UpdateBookAsync(
        long id,
        UpdateBookRequest request,
        CancellationToken cancellationToken = default)
    {
        var book = await dbContext.Books
            .Include(item => item.BookAuthors)
            .Include(item => item.BookCategories)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (book is null)
        {
            return null;
        }

        var title = NormalizeRequired(request.Title, 300, nameof(request.Title));
        var isbn = NormalizeOptional(request.Isbn, 20, nameof(request.Isbn));
        await ValidateIsbnAvailableAsync(isbn, id, cancellationToken);
        var authors = await LoadAuthorsAsync(request.AuthorIds, cancellationToken);
        var categories = await LoadCategoriesAsync(request.CategoryIds, cancellationToken);
        await ValidatePublisherAsync(request.PublisherId, cancellationToken);

        dbContext.Entry(book).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        book.Title = title;
        book.Isbn = isbn;
        book.Edition = NormalizeOptional(request.Edition, 100, nameof(request.Edition));
        book.PublicationYear = request.PublicationYear;
        book.Description = NormalizeOptional(request.Description, 4000, nameof(request.Description));
        book.PublisherId = request.PublisherId;
        book.UpdatedAtUtc = DateTime.UtcNow;

        dbContext.BookAuthors.RemoveRange(book.BookAuthors);
        dbContext.BookCategories.RemoveRange(book.BookCategories);
        book.BookAuthors = authors
            .Select(author => new BookAuthor { Book = book, Author = author, AuthorId = author.Id })
            .ToList();
        book.BookCategories = categories
            .Select(category => new BookCategory { Book = book, Category = category, CategoryId = category.Id })
            .ToList();

        AddAudit("BookUpdated", nameof(Book), book.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);
        return await GetRequiredBookAsync(book.Id, cancellationToken);
    }

    public async Task<bool> SetBookArchivedAsync(
        long id,
        bool isArchived,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        var book = await dbContext.Books.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (book is null)
        {
            return false;
        }

        dbContext.Entry(book).Property(item => item.RowVersion).OriginalValue = rowVersion;
        book.IsArchived = isArchived;
        book.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit(isArchived ? "BookArchived" : "BookRestored", nameof(Book), book.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> SetCoverAsync(
        long id,
        Stream content,
        string fileName,
        string contentType,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        var book = await dbContext.Books.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (book is null)
        {
            return null;
        }

        var newPath = await coverStorage.SaveAsync(
            content,
            fileName,
            contentType,
            cancellationToken);
        var previousPath = book.CoverImagePath;

        try
        {
            dbContext.Entry(book).Property(item => item.RowVersion).OriginalValue = rowVersion;
            book.CoverImagePath = newPath;
            book.UpdatedAtUtc = DateTime.UtcNow;
            AddAudit("BookCoverUpdated", nameof(Book), book.Id, null);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await coverStorage.DeleteAsync(newPath, CancellationToken.None);
            throw;
        }

        if (previousPath is not null)
        {
            await coverStorage.DeleteAsync(previousPath, cancellationToken);
        }

        return newPath;
    }

    public async Task<BookCopyDto> AddCopyAsync(
        long bookId,
        CreateBookCopyRequest request,
        CancellationToken cancellationToken = default)
    {
        var book = await dbContext.Books.SingleOrDefaultAsync(item => item.Id == bookId, cancellationToken)
            ?? throw new ValidationException("The selected book does not exist.");
        if (book.IsArchived)
        {
            throw new ValidationException("Copies cannot be added to an archived book.");
        }

        var barcode = NormalizeRequired(request.Barcode, 100, nameof(request.Barcode));
        if (await dbContext.BookCopies.AnyAsync(copy => copy.Barcode == barcode, cancellationToken))
        {
            throw new ValidationException("A copy with this barcode already exists.");
        }

        if (request.ShelfLocationId.HasValue &&
            !await dbContext.ShelfLocations.AnyAsync(
                shelf => shelf.Id == request.ShelfLocationId.Value,
                cancellationToken))
        {
            throw new ValidationException("The selected shelf does not exist.");
        }

        if (!Enum.IsDefined(request.Condition))
        {
            throw new ValidationException("The selected book condition is invalid.");
        }

        var copy = new BookCopy
        {
            Book = book,
            BookId = book.Id,
            Barcode = barcode,
            ShelfLocationId = request.ShelfLocationId,
            Condition = request.Condition,
            AcquisitionDate = request.AcquisitionDate,
            AcquisitionPrice = request.AcquisitionPrice
        };
        dbContext.BookCopies.Add(copy);
        await dbContext.SaveChangesAsync(cancellationToken);
        AddAudit("BookCopyCreated", nameof(BookCopy), copy.Id, $"Book ID: {book.Id}");
        await dbContext.SaveChangesAsync(cancellationToken);

        return await MapCopyAsync(copy.Id, cancellationToken);
    }

    public async Task<bool> WithdrawCopyAsync(
        long copyId,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        var copy = await dbContext.BookCopies.SingleOrDefaultAsync(item => item.Id == copyId, cancellationToken);
        if (copy is null)
        {
            return false;
        }

        if (copy.Status is BookCopyStatus.OnLoan or BookCopyStatus.Reserved)
        {
            throw new ValidationException("A loaned or reserved copy cannot be withdrawn.");
        }

        dbContext.Entry(copy).Property(item => item.RowVersion).OriginalValue = rowVersion;
        copy.Status = BookCopyStatus.Withdrawn;
        copy.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit("BookCopyWithdrawn", nameof(BookCopy), copy.Id, null);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Book> CompleteBookQuery() => dbContext.Books
        .Include(book => book.Publisher)
        .Include(book => book.BookAuthors).ThenInclude(item => item.Author)
        .Include(book => book.BookCategories).ThenInclude(item => item.Category)
        .Include(book => book.Copies)
        .AsSplitQuery();

    private async Task<BookDto> GetRequiredBookAsync(long id, CancellationToken cancellationToken) =>
        await GetBookAsync(id, cancellationToken)
        ?? throw new InvalidOperationException("The saved book could not be reloaded.");

    private async Task<BookCopyDto> MapCopyAsync(long id, CancellationToken cancellationToken)
    {
        var copy = await dbContext.BookCopies.AsNoTracking()
            .Include(item => item.ShelfLocation)
            .SingleAsync(item => item.Id == id, cancellationToken);
        return new BookCopyDto
        {
            Id = copy.Id,
            BookId = copy.BookId,
            Barcode = copy.Barcode,
            ShelfCode = copy.ShelfLocation?.Code,
            Status = copy.Status,
            Condition = copy.Condition,
            AcquisitionDate = copy.AcquisitionDate,
            AcquisitionPrice = copy.AcquisitionPrice,
            RowVersion = copy.RowVersion
        };
    }

    private async Task<List<Author>> LoadAuthorsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToArray();
        var authors = await dbContext.Authors
            .Where(author => distinctIds.Contains(author.Id))
            .ToListAsync(cancellationToken);
        if (authors.Count != distinctIds.Length)
        {
            throw new ValidationException("One or more selected authors do not exist.");
        }

        return authors;
    }

    private async Task<List<Category>> LoadCategoriesAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToArray();
        var categories = await dbContext.Categories
            .Where(category => distinctIds.Contains(category.Id))
            .ToListAsync(cancellationToken);
        if (categories.Count != distinctIds.Length)
        {
            throw new ValidationException("One or more selected categories do not exist.");
        }

        return categories;
    }

    private async Task ValidatePublisherAsync(int? publisherId, CancellationToken cancellationToken)
    {
        if (publisherId.HasValue &&
            !await dbContext.Publishers.AnyAsync(item => item.Id == publisherId.Value, cancellationToken))
        {
            throw new ValidationException("The selected publisher does not exist.");
        }
    }

    private async Task ValidateIsbnAvailableAsync(
        string? isbn,
        long? excludedBookId,
        CancellationToken cancellationToken)
    {
        if (isbn is not null && await dbContext.Books.AnyAsync(
            book => book.Isbn == isbn && book.Id != excludedBookId,
            cancellationToken))
        {
            throw new ValidationException("A book with this ISBN already exists.");
        }
    }

    private void AddAudit(string action, string targetType, long targetId, string? details)
    {
        dbContext.AuditLogs.Add(new AuditLog
        {
            ActorUserId = currentUser.UserId,
            Action = action,
            TargetType = targetType,
            TargetId = targetId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Details = details
        });
    }

    private static BookDto MapBook(Book book) => new()
    {
        Id = book.Id,
        Isbn = book.Isbn,
        Title = book.Title,
        Edition = book.Edition,
        PublicationYear = book.PublicationYear,
        Description = book.Description,
        Publisher = book.Publisher?.Name,
        CoverImagePath = book.CoverImagePath,
        Authors = book.BookAuthors.Select(item => item.Author.Name).Order().ToArray(),
        Categories = book.BookCategories.Select(item => item.Category.Name).Order().ToArray(),
        TotalCopies = book.Copies.Count,
        AvailableCopies = book.Copies.Count(copy => copy.Status == BookCopyStatus.Available),
        IsArchived = book.IsArchived,
        RowVersion = book.RowVersion
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
