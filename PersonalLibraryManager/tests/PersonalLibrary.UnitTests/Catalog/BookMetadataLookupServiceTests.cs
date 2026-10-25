using System.Data;
using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Application.Catalog.Services;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Catalog;
using PersonalLibrary.Domain.Common;

namespace PersonalLibrary.UnitTests.Catalog;

public sealed class BookMetadataLookupServiceTests
{
    private const string ValidIsbn = "9780451524935";

    [Fact]
    public async Task PreviewByIsbnAsync_returns_local_edition_before_calling_provider()
    {
        var author = new Author { Id = Guid.NewGuid(), Name = "George Orwell" };
        var book = new Book { Id = Guid.NewGuid(), Title = "Nineteen Eighty-Four" };
        book.BookAuthors.Add(new BookAuthor
        {
            BookId = book.Id, AuthorId = author.Id, Book = book, Author = author, AuthorOrder = 1
        });
        var edition = new BookEdition
        {
            Id = Guid.NewGuid(), BookId = book.Id, Book = book, Isbn13 = ValidIsbn
        };
        var provider = new StubMetadataProvider(null);
        var service = CreateService(new StubEditionRepository(edition), provider);

        var result = await service.PreviewByIsbnAsync(ValidIsbn);

        Assert.True(result.FoundInLocalCatalog);
        Assert.Equal(edition.Id, result.BookEditionId);
        Assert.Equal("Nineteen Eighty-Four", result.Metadata.Title);
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task PreviewByIsbnAsync_returns_external_metadata_without_persisting_it()
    {
        var metadata = CreateMetadata();
        var provider = new StubMetadataProvider(metadata);
        var editions = new StubEditionRepository(null);
        var books = new StubBookRepository();
        var unitOfWork = new StubUnitOfWork();
        var service = new BookMetadataLookupService(
            editions, books, new StubAuthorRepository(), new StubPublisherRepository(), provider, unitOfWork);

        var result = await service.PreviewByIsbnAsync(ValidIsbn);

        Assert.False(result.FoundInLocalCatalog);
        Assert.Null(result.BookId);
        Assert.Null(result.BookEditionId);
        Assert.Equal(metadata, result.Metadata);
        Assert.Equal(ValidIsbn, provider.LastIsbn);
        Assert.Null(books.Added);
        Assert.Equal(0, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task ImportAsync_persists_reviewed_metadata_when_edition_is_not_local()
    {
        var metadata = CreateMetadata();
        var provider = new StubMetadataProvider(metadata);
        var editions = new StubEditionRepository(null);
        var books = new StubBookRepository();
        var authors = new StubAuthorRepository();
        var publishers = new StubPublisherRepository();
        var unitOfWork = new StubUnitOfWork();
        var service = new BookMetadataLookupService(
            editions, books, authors, publishers, provider, unitOfWork);

        var result = await service.ImportAsync("978-0-451-52493-5", metadata, false);

        Assert.False(result.FoundInLocalCatalog);
        Assert.Equal(metadata, result.Metadata);
        Assert.Equal(0, provider.Calls);
        Assert.Equal(books.Added!.Id, result.BookId);
        Assert.Equal(editions.Added!.Id, result.BookEditionId);
        Assert.Equal(ValidIsbn, editions.Added.Isbn13);
        Assert.Equal("Signet Classics", publishers.Added!.Name);
        Assert.Equal("George Orwell", authors.Added!.Name);
        Assert.Equal("English novelist.", authors.Added.Biography);
        Assert.Equal(new DateOnly(1903, 6, 25), authors.Added.BirthDate);
        Assert.Equal(new DateOnly(1950, 1, 21), authors.Added.DeathDate);
        Assert.Equal(1, unitOfWork.TransactionCalls);
    }

    [Fact]
    public async Task ImportAsync_reuses_local_edition_without_requiring_metadata()
    {
        var author = new Author { Id = Guid.NewGuid(), Name = "George Orwell" };
        var book = new Book { Id = Guid.NewGuid(), Title = "Nineteen Eighty-Four" };
        book.BookAuthors.Add(new BookAuthor
        {
            BookId = book.Id, AuthorId = author.Id, Book = book, Author = author, AuthorOrder = 1
        });
        var edition = new BookEdition
        {
            Id = Guid.NewGuid(), BookId = book.Id, Book = book, Isbn13 = ValidIsbn
        };
        var service = CreateService(
            new StubEditionRepository(edition),
            new StubMetadataProvider(null));

        var result = await service.ImportAsync(ValidIsbn, null, false);

        Assert.True(result.FoundInLocalCatalog);
        Assert.Equal(edition.Id, result.BookEditionId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567890")]
    [InlineData("9780451524936")]
    public async Task PreviewByIsbnAsync_rejects_invalid_isbn(string isbn)
    {
        var provider = new StubMetadataProvider(CreateMetadata());
        var service = CreateService(new StubEditionRepository(null), provider);

        await Assert.ThrowsAsync<CatalogValidationException>(() => service.PreviewByIsbnAsync(isbn));
        Assert.Equal(0, provider.Calls);
    }

    [Fact]
    public async Task ImportAsync_requires_explicit_confirmation_for_web_metadata()
    {
        var metadata = CreateMetadata() with
        {
            Provenance = new BookMetadataProvenance(
                "OpenAIWebSearch",
                null,
                "https://example.com/book",
                true,
                [new BookMetadataSourceReference("https://example.com/book")])
        };
        var service = CreateService(new StubEditionRepository(null), new StubMetadataProvider(null));

        var exception = await Assert.ThrowsAsync<CatalogValidationException>(() =>
            service.ImportAsync(ValidIsbn, metadata, false));

        Assert.Equal("ConfirmWebMetadata", exception.Field);
    }

    private static BookMetadataLookupService CreateService(
        StubEditionRepository editions,
        StubMetadataProvider provider) =>
        new(
            editions,
            new StubBookRepository(),
            new StubAuthorRepository(),
            new StubPublisherRepository(),
            provider,
            new StubUnitOfWork());

    private static BookMetadata CreateMetadata() =>
        new(
            "Nineteen Eighty-Four",
            null,
            "A dystopian novel.",
            "en",
            1949,
            [new BookMetadataAuthor("George Orwell", Biography: "English novelist.", BirthDate: "1903-06-25", DeathDate: "1950-01-21")],
            new BookEditionMetadata(null, ValidIsbn, "Paperback", "Signet Classics", "1950-07-01", 328, null),
            new BookMetadataProvenance("Test", null, null));

    private sealed class StubMetadataProvider(BookMetadata? result) : IBookMetadataProvider
    {
        public int Calls { get; private set; }
        public string? LastIsbn { get; private set; }

        public Task<BookMetadata?> GetByIsbnAsync(string isbn, CancellationToken cancellationToken = default)
        {
            Calls++;
            LastIsbn = isbn;
            return Task.FromResult(result);
        }
    }

    private sealed class StubEditionRepository(BookEdition? existing)
        : UnsupportedRepository<BookEdition>, IBookEditionRepository
    {
        public BookEdition? Added { get; private set; }
        public Task<BookEdition?> GetByIsbnWithDetailsAsync(string isbn, CancellationToken cancellationToken = default) =>
            Task.FromResult(existing);
        public Task<BookEdition?> GetByIdWithDetailsAsync(Guid editionId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<List<BookEdition>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public override void Add(BookEdition entity) => Added = entity;
    }

    private sealed class StubBookRepository : UnsupportedRepository<Book>, IBookRepository
    {
        public Book? Added { get; private set; }
        public Task<List<Book>> GetAllByTitleForUpdateAsync(string title, CancellationToken cancellationToken = default) =>
            Task.FromResult(new List<Book>());
        public Task<Book?> GetByIdWithDetailsAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Book?> GetForUpdateAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<List<Book>> GetAllByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public override void Add(Book entity) => Added = entity;
    }

    private sealed class StubAuthorRepository : UnsupportedRepository<Author>, IAuthorRepository
    {
        public Author? Added { get; private set; }
        public Task<Author?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult<Author?>(null);
        public Task<List<Author>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public override void Add(Author entity) => Added = entity;
    }

    private sealed class StubPublisherRepository : UnsupportedRepository<Publisher>, IPublisherRepository
    {
        public Publisher? Added { get; private set; }
        public Task<Publisher?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
            Task.FromResult<Publisher?>(null);
        public Task<List<Publisher>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public override void Add(Publisher entity) => Added = entity;
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int TransactionCalls { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public async Task<int> ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
            TransactionCalls++;
            await operation(cancellationToken);
            return 1;
        }
    }

    private abstract class UnsupportedRepository<T> : IRepository<T> where T : AuditableEntity
    {
        public ValueTask<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<PagedResult<T>> GetPageAsync(PageRequest request, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public virtual void Add(T entity) => throw new NotSupportedException();
        public void Update(T entity) => throw new NotSupportedException();
        public void Remove(T entity) => throw new NotSupportedException();
    }
}
