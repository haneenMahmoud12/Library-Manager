using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Infrastructure.Persistence;

/// <summary>
/// Imports a small, explicit set of ISBNs from Open Library into the catalog.
/// This is intended for development/test seeding, not bulk catalog ingestion.
/// </summary>
public static class OpenLibraryDatabaseSeeder
{
    private const int MaximumIsbnsPerRequest = 20;
    private const int MaximumSearchPageSize = 100;
    private const int MaximumSearchBooks = 1_000;
    private static readonly Uri BooksApiUri = new("https://openlibrary.org/api/books");
    private static readonly Uri SearchApiUri = new("https://openlibrary.org/search.json");

    public static async Task<OpenLibrarySeedResult> SeedFromSearchAsync(
        ApplicationDbContext context,
        HttpClient httpClient,
        string query,
        int maximumBooks = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(httpClient);

        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentException("An Open Library search query is required.", nameof(query));
        if (maximumBooks is < 1 or > MaximumSearchBooks)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maximumBooks),
                $"The number of books must be between 1 and {MaximumSearchBooks}.");
        }

        var isbns = await FindIsbnsAsync(
            httpClient,
            query.Trim(),
            maximumBooks,
            cancellationToken);

        return await SeedAsync(context, httpClient, isbns, cancellationToken);
    }

    public static async Task<OpenLibrarySeedResult> SeedAsync(
        ApplicationDbContext context,
        HttpClient httpClient,
        IEnumerable<string> isbns,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(isbns);

        var normalizedIsbns = isbns
            .Select(IsbnNormalizer.Normalize)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (normalizedIsbns.Length == 0)
            return new OpenLibrarySeedResult(0, 0, 0, 0, 0, 0, 0);

        var importedBooks = await FetchBooksAsync(
            httpClient,
            normalizedIsbns,
            cancellationToken);

        var authorsByName = (await context.Authors.ToListAsync(cancellationToken))
            .GroupBy(author => author.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var publishersByName = (await context.Publishers.ToListAsync(cancellationToken))
            .GroupBy(publisher => publisher.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
        var books = await context.Books
            .Include(book => book.BookAuthors)
            .ThenInclude(bookAuthor => bookAuthor.Author)
            .ToListAsync(cancellationToken);
        var existingEditionIsbns = (await context.BookEditions
                .Select(edition => new { edition.Isbn10, edition.Isbn13 })
                .ToListAsync(cancellationToken))
            .SelectMany(edition => new[] { edition.Isbn10, edition.Isbn13 })
            .Where(isbn => isbn is not null)
            .Select(isbn => isbn!)
            .ToHashSet(StringComparer.Ordinal);

        var booksAdded = 0;
        var editionsAdded = 0;
        var authorsAdded = 0;
        var bookAuthorsAdded = 0;
        var publishersAdded = 0;
        var foundIsbns = new HashSet<string>(StringComparer.Ordinal);

        foreach (var requestedIsbn in normalizedIsbns)
        {
            if (!importedBooks.TryGetValue($"ISBN:{requestedIsbn}", out var importedBook) ||
                string.IsNullOrWhiteSpace(importedBook.Title))
            {
                continue;
            }

            foundIsbns.Add(requestedIsbn);
            var isbn10 = FirstValidIsbn(importedBook.Identifiers?.Isbn10, 10);
            var isbn13 = FirstValidIsbn(importedBook.Identifiers?.Isbn13, 13);
            if (requestedIsbn.Length == 10)
                isbn10 ??= requestedIsbn;
            else
                isbn13 ??= requestedIsbn;

            if ((isbn10 is not null && existingEditionIsbns.Contains(isbn10)) ||
                (isbn13 is not null && existingEditionIsbns.Contains(isbn13)))
            {
                continue;
            }

            var importedAuthors = new List<Author>();
            foreach (var authorName in (importedBook.Authors ?? [])
                         .Select(author => Limit(author.Name, 300))
                         .Where(name => name is not null)
                         .Select(name => name!)
                         .Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!authorsByName.TryGetValue(authorName, out var author))
                {
                    author = new Author
                    {
                        Id = Guid.NewGuid(),
                        Name = authorName,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.Authors.Add(author);
                    authorsByName.Add(authorName, author);
                    authorsAdded++;
                }
                importedAuthors.Add(author);
            }

            var title = Limit(importedBook.Title, 500)!;
            var book = books.FirstOrDefault(candidate =>
                string.Equals(candidate.Title, title, StringComparison.OrdinalIgnoreCase) &&
                AuthorsMatch(candidate, importedAuthors));
            if (book is null)
            {
                book = new Book
                {
                    Id = Guid.NewGuid(),
                    Title = title,
                    OriginalLanguageCode = GetLanguageCode(importedBook.Languages),
                    FirstPublishedYear = ParseYear(importedBook.PublishDate),
                    CreatedAt = DateTime.UtcNow
                };

                for (var index = 0; index < importedAuthors.Count; index++)
                {
                    var author = importedAuthors[index];
                    book.BookAuthors.Add(new BookAuthor
                    {
                        BookId = book.Id,
                        AuthorId = author.Id,
                        AuthorOrder = index + 1,
                        Book = book,
                        Author = author
                    });
                    bookAuthorsAdded++;
                }

                context.Books.Add(book);
                books.Add(book);
                booksAdded++;
            }

            Publisher? publisher = null;
            var publisherName = (importedBook.Publishers ?? [])
                .Select(item => Limit(item.Name, 300))
                .FirstOrDefault(name => name is not null);
            if (publisherName is not null &&
                !publishersByName.TryGetValue(publisherName, out publisher))
            {
                publisher = new Publisher
                {
                    Id = Guid.NewGuid(),
                    Name = publisherName,
                    CreatedAt = DateTime.UtcNow
                };
                context.Publishers.Add(publisher);
                publishersByName.Add(publisherName, publisher);
                publishersAdded++;
            }

            var edition = new BookEdition
            {
                Id = Guid.NewGuid(),
                BookId = book.Id,
                PublisherId = publisher?.Id,
                Isbn10 = isbn10,
                Isbn13 = isbn13,
                EditionName = Limit(importedBook.EditionName, 200),
                LanguageCode = GetLanguageCode(importedBook.Languages),
                PublicationDate = ParseDate(importedBook.PublishDate),
                PageCount = importedBook.NumberOfPages is > 0 ? importedBook.NumberOfPages : null,
                CoverImageUrl = Limit(importedBook.Cover?.Large ?? importedBook.Cover?.Medium, 1000),
                CreatedAt = DateTime.UtcNow,
                Book = book,
                Publisher = publisher
            };
            context.BookEditions.Add(edition);
            if (isbn10 is not null)
                existingEditionIsbns.Add(isbn10);
            if (isbn13 is not null)
                existingEditionIsbns.Add(isbn13);
            editionsAdded++;
        }

        await context.SaveChangesAsync(cancellationToken);

        return new OpenLibrarySeedResult(
            normalizedIsbns.Length,
            foundIsbns.Count,
            booksAdded,
            editionsAdded,
            authorsAdded,
            bookAuthorsAdded,
            publishersAdded);
    }

    private static async Task<Dictionary<string, OpenLibraryBook>> FetchBooksAsync(
        HttpClient httpClient,
        IReadOnlyList<string> isbns,
        CancellationToken cancellationToken)
    {
        var result = new Dictionary<string, OpenLibraryBook>(StringComparer.Ordinal);
        foreach (var batch in isbns.Chunk(MaximumIsbnsPerRequest))
        {
            var bibKeys = string.Join(',', batch.Select(isbn => $"ISBN:{isbn}"));
            var requestUri = new UriBuilder(BooksApiUri)
            {
                Query = $"bibkeys={Uri.EscapeDataString(bibKeys)}&format=json&jscmd=data"
            }.Uri;

            using var request = CreateRequest(requestUri, httpClient);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var books = await response.Content.ReadFromJsonAsync<Dictionary<string, OpenLibraryBook>>(
                cancellationToken);
            if (books is null)
                continue;

            foreach (var (bibKey, book) in books)
                result[bibKey] = book;
        }

        return result;
    }

    private static async Task<IReadOnlyList<string>> FindIsbnsAsync(
        HttpClient httpClient,
        string query,
        int maximumBooks,
        CancellationToken cancellationToken)
    {
        var isbns = new HashSet<string>(StringComparer.Ordinal);
        var offset = 0;

        while (isbns.Count < maximumBooks)
        {
            var limit = Math.Min(MaximumSearchPageSize, maximumBooks - isbns.Count);
            var requestUri = new UriBuilder(SearchApiUri)
            {
                Query = $"q={Uri.EscapeDataString(query)}&fields=isbn&limit={limit}&offset={offset}"
            }.Uri;

            using var request = CreateRequest(requestUri, httpClient);
            using var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            var searchResult = await response.Content.ReadFromJsonAsync<OpenLibrarySearchResult>(
                cancellationToken);
            if (searchResult?.Documents is not { Count: > 0 } documents)
                break;

            foreach (var document in documents)
            {
                var isbn = document.Isbns?
                    .Select(value => IsbnNormalizer.TryNormalize(value, out var normalized)
                        ? normalized
                        : null)
                    .Where(value => value is not null)
                    .OrderByDescending(value => value!.Length)
                    .FirstOrDefault();
                if (isbn is not null)
                    isbns.Add(isbn);
                if (isbns.Count == maximumBooks)
                    break;
            }

            offset += documents.Count;
            if (offset >= searchResult.NumberFound)
                break;
        }

        return isbns.ToArray();
    }

    private static HttpRequestMessage CreateRequest(Uri requestUri, HttpClient httpClient)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        if (httpClient.DefaultRequestHeaders.UserAgent.Count == 0)
            request.Headers.UserAgent.ParseAdd("PersonalLibraryManager/1.0");
        return request;
    }

    private static bool AuthorsMatch(Book book, IReadOnlyCollection<Author> authors)
    {
        if (authors.Count == 0)
            return book.BookAuthors.Count == 0;

        var existingNames = book.BookAuthors
            .Select(bookAuthor => bookAuthor.Author.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var importedNames = authors
            .Select(author => author.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return existingNames.SetEquals(importedNames);
    }

    private static string? FirstValidIsbn(IEnumerable<string>? values, int length)
    {
        if (values is null)
            return null;

        foreach (var value in values)
        {
            if (IsbnNormalizer.TryNormalize(value, out var isbn) && isbn!.Length == length)
                return isbn;
        }
        return null;
    }

    private static string? GetLanguageCode(IEnumerable<OpenLibraryKeyedValue>? languages) =>
        languages?
            .Select(language => language.Key?.Trim('/').Split('/').LastOrDefault())
            .FirstOrDefault(code => !string.IsNullOrWhiteSpace(code));

    private static int? ParseYear(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        for (var index = 0; index <= value.Length - 4; index++)
        {
            if (int.TryParse(value.AsSpan(index, 4), out var year) && year is >= 1 and <= 9999)
                return year;
        }
        return null;
    }

    private static DateOnly? ParseDate(string? value) =>
        DateOnly.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var date)
            ? date
            : null;

    private static string? Limit(string? value, int maximumLength)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        return normalized?.Length > maximumLength ? normalized[..maximumLength] : normalized;
    }

    private sealed class OpenLibraryBook
    {
        [JsonPropertyName("title")] public string? Title { get; init; }
        [JsonPropertyName("edition_name")] public string? EditionName { get; init; }
        [JsonPropertyName("publish_date")] public string? PublishDate { get; init; }
        [JsonPropertyName("number_of_pages")] public int? NumberOfPages { get; init; }
        [JsonPropertyName("authors")] public List<OpenLibraryNamedValue>? Authors { get; init; }
        [JsonPropertyName("publishers")] public List<OpenLibraryNamedValue>? Publishers { get; init; }
        [JsonPropertyName("languages")] public List<OpenLibraryKeyedValue>? Languages { get; init; }
        [JsonPropertyName("identifiers")] public OpenLibraryIdentifiers? Identifiers { get; init; }
        [JsonPropertyName("cover")] public OpenLibraryCover? Cover { get; init; }
    }

    private sealed class OpenLibrarySearchResult
    {
        [JsonPropertyName("numFound")] public int NumberFound { get; init; }
        [JsonPropertyName("docs")] public List<OpenLibrarySearchDocument>? Documents { get; init; }
    }

    private sealed class OpenLibrarySearchDocument
    {
        [JsonPropertyName("isbn")] public List<string>? Isbns { get; init; }
    }

    private sealed class OpenLibraryNamedValue
    {
        [JsonPropertyName("name")] public string? Name { get; init; }
    }

    private sealed class OpenLibraryKeyedValue
    {
        [JsonPropertyName("key")] public string? Key { get; init; }
    }

    private sealed class OpenLibraryIdentifiers
    {
        [JsonPropertyName("isbn_10")] public List<string>? Isbn10 { get; init; }
        [JsonPropertyName("isbn_13")] public List<string>? Isbn13 { get; init; }
    }

    private sealed class OpenLibraryCover
    {
        [JsonPropertyName("medium")] public string? Medium { get; init; }
        [JsonPropertyName("large")] public string? Large { get; init; }
    }
}

public sealed record OpenLibrarySeedResult(
    int Requested,
    int Found,
    int BooksAdded,
    int EditionsAdded,
    int AuthorsAdded,
    int BookAuthorsAdded,
    int PublishersAdded);
