using System.ComponentModel.DataAnnotations;
using LibrarySystem.Application.Catalog;
using LibrarySystem.Application.Common;
using LibrarySystem.Application.Security;
using LibrarySystem.Domain.Catalog;
using LibrarySystem.Infrastructure.Auditing;
using LibrarySystem.Infrastructure.Catalog;
using LibrarySystem.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services.Catalog;

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
        ValidateIsbnFormat(isbn);
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
                book.BookCategories.Any(item => item.Category.Name.Contains(term)) ||
                (book.Publisher != null && book.Publisher.Name.Contains(term)) ||
                book.Copies.Any(copy => copy.ShelfLocation != null && copy.ShelfLocation.Code.Contains(term)) ||
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
            .Include(book => book.Copies).ThenInclude(copy => copy.ShelfLocation)
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
        ValidateIsbnFormat(isbn);
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

        await ValidateShelfAsync(request.ShelfLocationId, cancellationToken);

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

    public async Task<BookCopyDto?> GetCopyAsync(long copyId, CancellationToken cancellationToken = default)
    {
        if (!await dbContext.BookCopies.AnyAsync(copy => copy.Id == copyId, cancellationToken))
        {
            return null;
        }

        return await MapCopyAsync(copyId, cancellationToken);
    }

    public async Task<BookCopyDto?> UpdateCopyAsync(
        long copyId,
        UpdateBookCopyRequest request,
        CancellationToken cancellationToken = default)
    {
        var copy = await dbContext.BookCopies.SingleOrDefaultAsync(item => item.Id == copyId, cancellationToken);
        if (copy is null)
        {
            return null;
        }

        var barcode = NormalizeRequired(request.Barcode, 100, nameof(request.Barcode));
        if (await dbContext.BookCopies.AnyAsync(
            item => item.Barcode == barcode && item.Id != copyId, cancellationToken))
        {
            throw new ValidationException("A copy with this barcode already exists.");
        }

        await ValidateShelfAsync(request.ShelfLocationId, cancellationToken);
        if (!Enum.IsDefined(request.Condition))
        {
            throw new ValidationException("The selected book condition is invalid.");
        }

        dbContext.Entry(copy).Property(item => item.RowVersion).OriginalValue = request.RowVersion;
        copy.Barcode = barcode;
        copy.ShelfLocationId = request.ShelfLocationId;
        copy.Condition = request.Condition;
        copy.AcquisitionDate = request.AcquisitionDate;
        copy.AcquisitionPrice = request.AcquisitionPrice;
        copy.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit("BookCopyUpdated", nameof(BookCopy), copy.Id, $"Book ID: {copy.BookId}");
        await dbContext.SaveChangesAsync(cancellationToken);
        return await MapCopyAsync(copy.Id, cancellationToken);
    }

    public async Task<bool> SetCopyStatusAsync(
        long copyId,
        BookCopyStatus status,
        byte[] rowVersion,
        CancellationToken cancellationToken = default)
    {
        var copy = await dbContext.BookCopies.SingleOrDefaultAsync(item => item.Id == copyId, cancellationToken);
        if (copy is null)
        {
            return false;
        }

        if (!Enum.IsDefined(status) || status is BookCopyStatus.OnLoan or BookCopyStatus.Reserved)
        {
            throw new ValidationException("This status can only be assigned by a circulation workflow.");
        }

        if (copy.Status is BookCopyStatus.OnLoan or BookCopyStatus.Reserved)
        {
            throw new ValidationException("A loaned or reserved copy cannot be changed through copy management.");
        }

        if (copy.Status == BookCopyStatus.Withdrawn && status != BookCopyStatus.Available)
        {
            throw new ValidationException("A withdrawn copy must be restored before another status can be assigned.");
        }

        if (status == BookCopyStatus.Available && copy.Condition == BookCondition.Damaged)
        {
            throw new ValidationException("Change the condition before making a damaged copy available.");
        }

        dbContext.Entry(copy).Property(item => item.RowVersion).OriginalValue = rowVersion;
        copy.Status = status;
        if (status == BookCopyStatus.Damaged)
        {
            copy.Condition = BookCondition.Damaged;
        }
        copy.UpdatedAtUtc = DateTime.UtcNow;
        AddAudit("BookCopyStatusChanged", nameof(BookCopy), copy.Id, $"Status: {status}");
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
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
        .Include(book => book.Copies).ThenInclude(copy => copy.ShelfLocation)
        .AsSplitQuery();

    private async Task<BookDto> GetRequiredBookAsync(long id, CancellationToken cancellationToken) =>
        await GetBookAsync(id, cancellationToken)
        ?? throw new InvalidOperationException("The saved book could not be reloaded.");

    private async Task<BookCopyDto> MapCopyAsync(long id, CancellationToken cancellationToken)
    {
        var copy = await dbContext.BookCopies.AsNoTracking()
            .Include(item => item.ShelfLocation)
            .Include(item => item.Loans).ThenInclude(loan => loan.Member)
            .SingleAsync(item => item.Id == id, cancellationToken);
        return new BookCopyDto
        {
            Id = copy.Id,
            BookId = copy.BookId,
            Barcode = copy.Barcode,
            ShelfCode = copy.ShelfLocation?.Code,
            ShelfLocationId = copy.ShelfLocationId,
            Status = copy.Status,
            Condition = copy.Condition,
            AcquisitionDate = copy.AcquisitionDate,
            AcquisitionPrice = copy.AcquisitionPrice,
            RowVersion = copy.RowVersion,
            History = copy.Loans.OrderByDescending(loan => loan.CheckedOutAtUtc).Select(loan => new BookCopyHistoryDto
            {
                LoanId = loan.Id,
                MemberNumber = loan.Member.MemberNumber,
                CheckedOutAtUtc = loan.CheckedOutAtUtc,
                DueAtUtc = loan.DueAtUtc,
                ReturnedAtUtc = loan.ReturnedAtUtc,
                Status = loan.Status.ToString()
            }).ToArray()
        };
    }

    private async Task ValidateShelfAsync(int? shelfId, CancellationToken cancellationToken)
    {
        if (shelfId.HasValue && !await dbContext.ShelfLocations.AnyAsync(
            shelf => shelf.Id == shelfId && shelf.IsActive, cancellationToken))
        {
            throw new ValidationException("The selected shelf does not exist or is inactive.");
        }
    }

    private async Task<List<Author>> LoadAuthorsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken)
    {
        var distinctIds = ids.Distinct().ToArray();
        var authors = await dbContext.Authors
            .Where(author => distinctIds.Contains(author.Id) && author.IsActive)
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
            .Where(category => distinctIds.Contains(category.Id) && category.IsActive)
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
            !await dbContext.Publishers.AnyAsync(item => item.Id == publisherId.Value && item.IsActive, cancellationToken))
        {
            throw new ValidationException("The selected publisher does not exist or is inactive.");
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
        PublisherId = book.PublisherId,
        CoverImagePath = book.CoverImagePath,
        Authors = book.BookAuthors.Select(item => item.Author.Name).Order().ToArray(),
        AuthorIds = book.BookAuthors.Select(item => item.AuthorId).ToArray(),
        Categories = book.BookCategories.Select(item => item.Category.Name).Order().ToArray(),
        CategoryIds = book.BookCategories.Select(item => item.CategoryId).ToArray(),
        TotalCopies = book.Copies.Count,
        AvailableCopies = book.Copies.Count(copy => copy.Status == BookCopyStatus.Available),
        OnLoanCopies = book.Copies.Count(copy => copy.Status == BookCopyStatus.OnLoan),
        ReservedCopies = book.Copies.Count(copy => copy.Status == BookCopyStatus.Reserved),
        LostCopies = book.Copies.Count(copy => copy.Status == BookCopyStatus.Lost),
        DamagedCopies = book.Copies.Count(copy => copy.Status == BookCopyStatus.Damaged),
        WithdrawnCopies = book.Copies.Count(copy => copy.Status == BookCopyStatus.Withdrawn),
        Copies = book.Copies.OrderBy(copy => copy.Barcode).Select(copy => new BookCopyDto
        {
            Id = copy.Id,
            BookId = copy.BookId,
            Barcode = copy.Barcode,
            ShelfCode = copy.ShelfLocation?.Code,
            ShelfLocationId = copy.ShelfLocationId,
            Status = copy.Status,
            Condition = copy.Condition,
            AcquisitionDate = copy.AcquisitionDate,
            AcquisitionPrice = copy.AcquisitionPrice,
            RowVersion = copy.RowVersion,
            History = []
        }).ToArray(),
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

    private static void ValidateIsbnFormat(string? isbn)
    {
        if (isbn is null)
        {
            return;
        }

        var compact = isbn.Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal);
        var valid = compact.Length switch
        {
            10 when compact.Take(9).All(char.IsDigit) && (char.IsDigit(compact[9]) || compact[9] is 'X' or 'x') =>
                compact.Take(9).Select((character, index) => (character - '0') * (10 - index)).Sum()
                + (compact[9] is 'X' or 'x' ? 10 : compact[9] - '0') is var sum && sum % 11 == 0,
            13 when compact.All(char.IsDigit) =>
                compact.Select((character, index) => (character - '0') * (index % 2 == 0 ? 1 : 3)).Sum() % 10 == 0,
            _ => false
        };
        if (!valid)
        {
            throw new ValidationException("Enter a valid ISBN-10 or ISBN-13 (hyphens are allowed).");
        }
    }
}
