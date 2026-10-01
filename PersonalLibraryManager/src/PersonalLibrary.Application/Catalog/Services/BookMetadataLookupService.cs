using System.Globalization;
using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Application.Catalog.Services;

public sealed class BookMetadataLookupService(
    IBookEditionRepository editionRepository,
    IBookRepository bookRepository,
    IAuthorRepository authorRepository,
    IPublisherRepository publisherRepository,
    IBookMetadataProvider metadataProvider,
    IUnitOfWork unitOfWork) : IBookMetadataLookupService
{
    public async Task<BookMetadataLookupResult> GetByIsbnAsync(
        string isbn,
        CancellationToken cancellationToken = default)
    {
        var normalizedIsbn = IsbnNormalizer.Normalize(isbn);
        var localEdition = await editionRepository.GetByIsbnWithDetailsAsync(
            normalizedIsbn,
            cancellationToken);

        if (localEdition is not null)
        {
            return new BookMetadataLookupResult(
                true,
                localEdition.BookId,
                localEdition.Id,
                MapLocal(localEdition));
        }

        var metadata = await metadataProvider.GetByIsbnAsync(normalizedIsbn, cancellationToken)
            ?? throw new BookMetadataNotFoundException(normalizedIsbn);
        var (bookId, editionId) = await PersistAsync(
            normalizedIsbn,
            metadata,
            cancellationToken);

        return new BookMetadataLookupResult(false, bookId, editionId, metadata);
    }

    private async Task<(Guid BookId, Guid EditionId)> PersistAsync(
        string requestedIsbn,
        BookMetadata metadata,
        CancellationToken cancellationToken)
    {
        Guid bookId = default;
        Guid editionId = default;

        await unitOfWork.ExecuteInTransactionAsync(
            async transactionCancellationToken =>
            {
                // Recheck inside the transaction in case another request imported it.
                var existingEdition = await editionRepository.GetByIsbnWithDetailsAsync(
                    requestedIsbn,
                    transactionCancellationToken);
                if (existingEdition is not null)
                {
                    bookId = existingEdition.BookId;
                    editionId = existingEdition.Id;
                    return;
                }

                var authors = await ResolveAuthorsAsync(metadata.Authors, transactionCancellationToken);
                var book = await ResolveBookAsync(metadata, authors, transactionCancellationToken);
                var publisher = await ResolvePublisherAsync(
                    metadata.Edition.PublisherName,
                    transactionCancellationToken);

                var isbn10 = NormalizeProviderIsbn(metadata.Edition.Isbn10, 10);
                var isbn13 = NormalizeProviderIsbn(metadata.Edition.Isbn13, 13);
                if (requestedIsbn.Length == 10)
                    isbn10 ??= requestedIsbn;
                else
                    isbn13 ??= requestedIsbn;

                var edition = new BookEdition
                {
                    Id = Guid.NewGuid(),
                    BookId = book.Id,
                    PublisherId = publisher?.Id,
                    Isbn10 = isbn10,
                    Isbn13 = isbn13,
                    EditionName = Limit(metadata.Edition.EditionName, 200),
                    LanguageCode = Limit(metadata.LanguageCode, 10),
                    PublicationDate = ParsePublicationDate(metadata.Edition.PublishedDate),
                    PageCount = metadata.Edition.PageCount is > 0 ? metadata.Edition.PageCount : null,
                    CoverImageUrl = Limit(metadata.Edition.CoverImageUrl, 1000),
                    Description = Normalize(metadata.Description),
                    CreatedAt = DateTime.UtcNow,
                    Book = book,
                    Publisher = publisher
                };

                editionRepository.Add(edition);
                bookId = book.Id;
                editionId = edition.Id;
            },
            cancellationToken: cancellationToken);

        return (bookId, editionId);
    }

    private async Task<List<Author>> ResolveAuthorsAsync(
        IReadOnlyList<BookMetadataAuthor> metadataAuthors,
        CancellationToken cancellationToken)
    {
        var authors = new List<Author>();
        foreach (var metadataAuthor in metadataAuthors
                     .Where(author => !string.IsNullOrWhiteSpace(author.Name))
                     .DistinctBy(author => author.Name.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var name = Limit(metadataAuthor.Name, 300)!;
            var biography = Normalize(metadataAuthor.Biography);
            var birthDate = ParsePersonDate(metadataAuthor.BirthDate);
            var deathDate = ParsePersonDate(metadataAuthor.DeathDate);
            var author = await authorRepository.GetByNameAsync(name, cancellationToken);
            if (author is null)
            {
                author = new Author
                {
                    Id = Guid.NewGuid(),
                    Name = name,
                    Biography = biography,
                    BirthDate = birthDate,
                    DeathDate = deathDate,
                    CreatedAt = DateTime.UtcNow
                };
                authorRepository.Add(author);
            }
            else
            {
                var changed = false;
                if (author.Biography is null && biography is not null)
                {
                    author.Biography = biography;
                    changed = true;
                }
                if (author.BirthDate is null && birthDate is not null)
                {
                    author.BirthDate = birthDate;
                    changed = true;
                }
                if (author.DeathDate is null && deathDate is not null)
                {
                    author.DeathDate = deathDate;
                    changed = true;
                }
                if (changed)
                    author.UpdatedAt = DateTime.UtcNow;
            }
            authors.Add(author);
        }

        return authors;
    }

    private async Task<Book> ResolveBookAsync(
        BookMetadata metadata,
        IReadOnlyList<Author> authors,
        CancellationToken cancellationToken)
    {
        var title = Limit(metadata.Title, 500)
            ?? throw new CatalogValidationException("Title", "The metadata provider returned no title.");
        var candidates = await bookRepository.GetAllByTitleForUpdateAsync(title, cancellationToken);
        var book = candidates.FirstOrDefault(candidate => AuthorsMatch(candidate, authors));

        if (book is null)
        {
            book = new Book
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = Normalize(metadata.Description),
                OriginalLanguageCode = Limit(metadata.LanguageCode, 10),
                FirstPublishedYear = ValidateYear(metadata.FirstPublishedYear),
                CreatedAt = DateTime.UtcNow
            };

            for (var index = 0; index < authors.Count; index++)
            {
                var author = authors[index];
                book.BookAuthors.Add(new BookAuthor
                {
                    BookId = book.Id,
                    AuthorId = author.Id,
                    AuthorOrder = index + 1,
                    Book = book,
                    Author = author
                });
            }
            bookRepository.Add(book);
            return book;
        }

        var changed = false;
        if (book.Description is null && Normalize(metadata.Description) is { } description)
        {
            book.Description = description;
            changed = true;
        }
        if (book.OriginalLanguageCode is null && Limit(metadata.LanguageCode, 10) is { } language)
        {
            book.OriginalLanguageCode = language;
            changed = true;
        }
        if (book.FirstPublishedYear is null && ValidateYear(metadata.FirstPublishedYear) is { } year)
        {
            book.FirstPublishedYear = year;
            changed = true;
        }
        if (changed)
            book.UpdatedAt = DateTime.UtcNow;

        return book;
    }

    private async Task<Publisher?> ResolvePublisherAsync(
        string? publisherName,
        CancellationToken cancellationToken)
    {
        var name = Limit(publisherName, 300);
        if (name is null)
            return null;

        var publisher = await publisherRepository.GetByNameAsync(name, cancellationToken);
        if (publisher is not null)
            return publisher;

        publisher = new Publisher
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        publisherRepository.Add(publisher);
        return publisher;
    }

    private static bool AuthorsMatch(Book book, IReadOnlyList<Author> authors)
    {
        if (authors.Count == 0)
            return true;

        var existingNames = book.BookAuthors
            .Select(bookAuthor => bookAuthor.Author.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requestedNames = authors
            .Select(author => author.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return existingNames.SetEquals(requestedNames);
    }

    private static string? NormalizeProviderIsbn(string? value, int expectedLength) =>
        IsbnNormalizer.TryNormalize(value, out var normalized) && normalized!.Length == expectedLength
            ? normalized
            : null;

    private static DateOnly? ParsePublicationDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 4)
            return null;
        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)
            ? date
            : null;
    }

    private static DateOnly? ParsePersonDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)
            ? date
            : null;
    }

    private static int? ValidateYear(int? year) =>
        year is >= 1 && year <= 9999 ? year : null;

    private static string? Limit(string? value, int maximumLength)
    {
        var normalized = Normalize(value);
        return normalized?.Length > maximumLength
            ? normalized[..maximumLength]
            : normalized;
    }

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static BookMetadata MapLocal(BookEdition edition)
    {
        var book = edition.Book;
        return new BookMetadata(
            book.Title,
            null,
            edition.Description ?? book.Description,
            edition.LanguageCode ?? book.OriginalLanguageCode,
            book.FirstPublishedYear,
            book.BookAuthors
                .OrderBy(bookAuthor => bookAuthor.AuthorOrder)
                .Select(bookAuthor => new BookMetadataAuthor(bookAuthor.Author.Name))
                .ToList(),
            new BookEditionMetadata(
                edition.Isbn10,
                edition.Isbn13,
                edition.EditionName,
                edition.Publisher?.Name,
                edition.PublicationDate?.ToString("yyyy-MM-dd"),
                edition.PageCount,
                edition.CoverImageUrl),
            new BookMetadataProvenance("LocalCatalog", edition.Id.ToString(), null));
    }
}
