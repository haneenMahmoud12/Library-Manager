using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Catalog.Models;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Application.Catalog.Services;

public sealed class BookService(
    IBookRepository bookRepository,
    IAuthorRepository authorRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser) : IBookService
{
    public async Task<BookViewModel> GetByIdAsync(
        Guid bookId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(bookId, "BookId");
        var book = await bookRepository.GetByIdWithDetailsAsync(bookId, cancellationToken)
            ?? throw new BookNotFoundException(bookId);
        return Map(book);
    }

    public async Task<List<BookViewModel>> GetAllByAuthorIdAsync(
        Guid authorId,
        CancellationToken cancellationToken = default)
    {
        ValidateId(authorId, "AuthorId");
        if (!await authorRepository.ExistsAsync(authorId, cancellationToken))
            throw new AuthorNotFoundException(authorId);

        var books = await bookRepository.GetAllByAuthorIdAsync(authorId, cancellationToken);
        return books.Select(Map).ToList();
    }

    public async Task<BookViewModel> SaveAsync(
        CreateBookDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        RequireAuthenticatedUser();

        var authors = await ValidateAndLoadAuthorsAsync(dto.Authors, cancellationToken);
        var book = new Book
        {
            Id = Guid.NewGuid(),
            Title = ValidateRequiredText(dto.Title, 500, "Title"),
            OriginalTitle = ValidateOptionalText(dto.OriginalTitle, 500, "OriginalTitle"),
            Description = NormalizeOptional(dto.Description),
            OriginalLanguageCode = ValidateOptionalText(dto.OriginalLanguageCode, 10, "OriginalLanguageCode"),
            FirstPublishedYear = ValidateYear(dto.FirstPublishedYear),
            CreatedAt = DateTime.UtcNow
        };

        SetAuthors(book, authors);
        bookRepository.Add(book);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(book);
    }

    public async Task<BookViewModel> SaveAsync(
        Guid bookId,
        UpdateBookDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateId(bookId, "BookId");
        RequireAuthenticatedUser();

        var authors = await ValidateAndLoadAuthorsAsync(dto.Authors, cancellationToken);
        var book = await bookRepository.GetForUpdateAsync(bookId, cancellationToken)
            ?? throw new BookNotFoundException(bookId);

        if (dto.Title is not null)
            book.Title = ValidateRequiredText(dto.Title, 500, "Title");
        if (dto.OriginalTitle is not null)
            book.OriginalTitle = ValidateOptionalText(dto.OriginalTitle, 500, "OriginalTitle");
        if (dto.Description is not null)
            book.Description = NormalizeOptional(dto.Description);
        if (dto.OriginalLanguageCode is not null)
            book.OriginalLanguageCode = ValidateOptionalText(dto.OriginalLanguageCode, 10, "OriginalLanguageCode");
        if (dto.FirstPublishedYear.HasValue)
            book.FirstPublishedYear = ValidateYear(dto.FirstPublishedYear);

        SetAuthors(book, authors);
        book.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Map(book);
    }

    private async Task<List<(Author Author, int Order)>> ValidateAndLoadAuthorsAsync(
        IReadOnlyCollection<BookAuthorDto>? authors,
        CancellationToken cancellationToken)
    {
        if (authors is null)
            throw new CatalogValidationException("Authors", "Authors must be supplied.");

        var result = new List<(Author, int)>(authors.Count);
        var seen = new HashSet<Guid>();
        foreach (var item in authors)
        {
            ValidateId(item.AuthorId, "Authors");
            if (item.AuthorOrder < 1)
                throw new CatalogValidationException("Authors", "Author order must be at least 1.");
            if (!seen.Add(item.AuthorId))
                throw new CatalogValidationException("Authors", "The same author cannot be assigned more than once.");

            var author = await authorRepository.GetByIdAsync(item.AuthorId, cancellationToken)
                ?? throw new AuthorNotFoundException(item.AuthorId);
            result.Add((author, item.AuthorOrder));
        }

        return result;
    }

    private static void SetAuthors(Book book, IEnumerable<(Author Author, int Order)> authors)
    {
        var requested = authors.ToDictionary(item => item.Author.Id);
        foreach (var existing in book.BookAuthors
                     .Where(bookAuthor => !requested.ContainsKey(bookAuthor.AuthorId))
                     .ToList())
        {
            book.BookAuthors.Remove(existing);
        }

        foreach (var (author, order) in requested.Values)
        {
            var existing = book.BookAuthors.SingleOrDefault(bookAuthor => bookAuthor.AuthorId == author.Id);
            if (existing is not null)
            {
                existing.AuthorOrder = order;
                existing.Author = author;
                continue;
            }

            book.BookAuthors.Add(new BookAuthor
            {
                BookId = book.Id,
                AuthorId = author.Id,
                AuthorOrder = order,
                Book = book,
                Author = author
            });
        }
    }

    private void RequireAuthenticatedUser()
    {
        if (currentUser.UserId is not { } userId || userId == Guid.Empty)
            throw new UnauthorizedAccessException("An authenticated user is required.");
    }

    private static string ValidateRequiredText(string? value, int maxLength, string field)
    {
        var normalized = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalized) || normalized.Length > maxLength)
            throw new CatalogValidationException(field, $"{field} is required and cannot exceed {maxLength} characters.");
        return normalized;
    }

    private static string? ValidateOptionalText(string? value, int maxLength, string field)
    {
        var normalized = NormalizeOptional(value);
        if (normalized?.Length > maxLength)
            throw new CatalogValidationException(field, $"{field} cannot exceed {maxLength} characters.");
        return normalized;
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static int? ValidateYear(int? year)
    {
        if (year is < 1 || year > DateTime.UtcNow.Year + 1)
            throw new CatalogValidationException("FirstPublishedYear", "First published year must be between 1 and next year.");
        return year;
    }

    private static void ValidateId(Guid id, string field)
    {
        if (id == Guid.Empty)
            throw new CatalogValidationException(field, $"A valid {field} is required.");
    }

    private static BookViewModel Map(Book book) =>
        new(
            book.Id,
            book.Title,
            book.OriginalTitle,
            book.Description,
            book.OriginalLanguageCode,
            book.FirstPublishedYear,
            book.BookAuthors
                .OrderBy(bookAuthor => bookAuthor.AuthorOrder)
                .Select(bookAuthor => new BookAuthorViewModel(
                    bookAuthor.AuthorId,
                    bookAuthor.Author.Name,
                    bookAuthor.AuthorOrder))
                .ToList(),
            book.Editions
                .Select(edition => new BookEditionSummaryViewModel(
                    edition.Id,
                    edition.PublisherId,
                    edition.Isbn10,
                    edition.Isbn13,
                    edition.EditionName,
                    edition.LanguageCode))
                .ToList(),
            book.CreatedAt,
            book.UpdatedAt);
}
