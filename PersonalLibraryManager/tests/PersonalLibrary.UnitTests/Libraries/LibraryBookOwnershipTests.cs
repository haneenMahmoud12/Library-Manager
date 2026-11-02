using System.Data;
using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Exceptions;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Application.Catalog.Services;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Application.Libraries.Repositories;
using PersonalLibrary.Application.Libraries.Services;
using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Catalog;
using PersonalLibrary.Domain.Catalog.Enums;
using PersonalLibrary.Domain.Common;
using PersonalLibrary.Domain.Libraries;
using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.UnitTests.Libraries;

public sealed class LibraryBookOwnershipTests
{
    private const string Isbn = "9780451524935";

    [Fact]
    public async Task CheckOwnershipAsync_returns_all_copies_for_the_library_and_edition()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId);
        var edition = CreateEdition();
        var copies = new List<BookCopy>
        {
            CreateCopy(library.Id, edition),
            CreateCopy(library.Id, edition)
        };
        var copyRepository = new StubBookCopyRepository(copies);
        var service = CreateService(library, edition, copyRepository, userId);

        var result = await service.CheckOwnershipAsync(library.Id, "978-0-451-52493-5");

        Assert.True(result.IsOwned);
        Assert.False(result.OwnsDifferentEdition);
        Assert.True(result.IsBookOwned);
        Assert.Equal("ExactEditionOwned", result.OwnershipStatus);
        Assert.Equal(2, result.CopyCount);
        Assert.Equal(edition.BookId, result.BookId);
        Assert.Equal(edition.Id, result.BookEditionId);
        Assert.Equal(2, result.Copies.Count);
        Assert.NotEqual(result.Copies[0].Id, result.Copies[1].Id);
        Assert.Equal(library.Id, copyRepository.LastLibraryId);
        Assert.Equal(edition.BookId, copyRepository.LastBookId);
    }

    [Fact]
    public async Task CheckOwnershipAsync_returns_not_owned_when_the_edition_has_no_library_copies()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId);
        var edition = CreateEdition();
        var service = CreateService(
            library,
            edition,
            new StubBookCopyRepository([]),
            userId);

        var result = await service.CheckOwnershipAsync(library.Id, Isbn);

        Assert.False(result.IsOwned);
        Assert.False(result.OwnsDifferentEdition);
        Assert.False(result.IsBookOwned);
        Assert.Equal("NotOwned", result.OwnershipStatus);
        Assert.Equal(0, result.CopyCount);
        Assert.Equal(edition.Id, result.BookEditionId);
        Assert.Empty(result.Copies);
    }

    [Fact]
    public async Task CheckOwnershipAsync_returns_unknown_when_the_isbn_is_not_in_the_catalogue()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId);
        var service = CreateService(
            library,
            CreateEdition(),
            new StubBookCopyRepository([]),
            userId);

        var result = await service.CheckOwnershipAsync(library.Id, "9780141036144");

        Assert.False(result.CanDetermineOwnership);
        Assert.Equal("UnknownEdition", result.OwnershipStatus);
        Assert.Null(result.BookId);
        Assert.Null(result.BookEditionId);
    }

    [Fact]
    public async Task CheckOwnershipAsync_looks_up_an_unknown_isbn_and_matches_an_owned_book()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId);
        var author = new Author { Id = Guid.NewGuid(), Name = "George Orwell" };
        var book = new Book { Id = Guid.NewGuid(), Title = "Nineteen Eighty-Four" };
        book.BookAuthors.Add(new BookAuthor
        {
            BookId = book.Id,
            AuthorId = author.Id,
            Book = book,
            Author = author,
            AuthorOrder = 1
        });
        var ownedEdition = CreateEdition(book);
        var copyRepository = new StubBookCopyRepository([CreateCopy(library.Id, ownedEdition)]);
        var lookupMetadata = new BookMetadata(
            book.Title,
            null,
            null,
            "en",
            1949,
            [new BookMetadataAuthor(author.Name)],
            new BookEditionMetadata(null, "9780141036144", null, null, null, null, null),
            new BookMetadataProvenance("Test", null, null));
        var service = CreateService(
            library,
            ownedEdition,
            copyRepository,
            userId,
            lookupMetadata,
            [book]);

        var result = await service.CheckOwnershipAsync(library.Id, "9780141036144");

        Assert.True(result.CanDetermineOwnership);
        Assert.False(result.IsOwned);
        Assert.True(result.OwnsDifferentEdition);
        Assert.Equal("DifferentEditionOwned", result.OwnershipStatus);
        Assert.Equal(book.Id, result.BookId);
        Assert.Null(result.BookEditionId);
        Assert.Equal(ownedEdition.Id, Assert.Single(result.DifferentEditionCopies).BookEditionId);
    }

    [Fact]
    public async Task CheckOwnershipAsync_returns_copies_of_different_editions_of_the_same_book()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId);
        var requestedEdition = CreateEdition();
        var ownedEdition = CreateEdition(requestedEdition.Book);
        ownedEdition.Isbn13 = "9780141036144";
        var ownedCopy = CreateCopy(library.Id, ownedEdition);
        var service = CreateService(
            library,
            requestedEdition,
            new StubBookCopyRepository([ownedCopy]),
            userId);

        var result = await service.CheckOwnershipAsync(library.Id, Isbn);

        Assert.False(result.IsOwned);
        Assert.True(result.OwnsDifferentEdition);
        Assert.True(result.IsBookOwned);
        Assert.Equal("DifferentEditionOwned", result.OwnershipStatus);
        Assert.Equal(0, result.CopyCount);
        Assert.Equal(1, result.DifferentEditionCopyCount);
        Assert.Empty(result.Copies);
        Assert.Equal(ownedEdition.Id, Assert.Single(result.DifferentEditionCopies).BookEditionId);
    }

    [Fact]
    public async Task CheckOwnershipAsync_rejects_a_user_who_is_not_a_library_member()
    {
        var library = CreateLibrary(Guid.NewGuid());
        var service = CreateService(
            library,
            CreateEdition(),
            new StubBookCopyRepository([]),
            Guid.NewGuid());

        await Assert.ThrowsAsync<LibraryAccessDeniedException>(() =>
            service.CheckOwnershipAsync(library.Id, Isbn));
    }

    [Fact]
    public async Task AddManuallyAsync_creates_a_distinct_copy_when_the_edition_already_exists()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId, LibraryMemberRole.Editor);
        var edition = CreateEdition();
        var existing = CreateCopy(library.Id, edition);
        var copyRepository = new StubBookCopyRepository([existing]);
        var service = CreateService(library, edition, copyRepository, userId);

        var result = await service.AddManuallyAsync(library.Id, CreateAddDto(edition.Id));

        Assert.NotNull(copyRepository.AddedCopy);
        Assert.NotEqual(existing.Id, result.Id);
        Assert.Equal(2, copyRepository.CopyCount);
    }

    [Fact]
    public async Task AddManuallyAsync_creates_a_copy_when_the_edition_is_new()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId, LibraryMemberRole.Owner);
        var edition = CreateEdition();
        var copyRepository = new StubBookCopyRepository([]);
        var service = CreateService(library, edition, copyRepository, userId);

        var result = await service.AddManuallyAsync(library.Id, CreateAddDto(edition.Id));

        Assert.NotNull(copyRepository.AddedCopy);
        Assert.Equal(result.Id, copyRepository.AddedCopy.Id);
    }

    [Fact]
    public async Task ImportAsync_reuses_a_local_edition_and_creates_one_copy_transactionally()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId, LibraryMemberRole.Editor);
        var edition = CreateEdition();
        var copyRepository = new StubBookCopyRepository([]);
        var service = CreateService(library, edition, copyRepository, userId);

        var result = await service.ImportAsync(
            library.Id,
            Isbn,
            new ImportBookToLibraryDto(
                null,
                false,
                null,
                BookFormat.Paperback,
                ReadingStatus.Unread,
                null,
                null,
                null,
                null,
                null,
                null,
                null));

        Assert.NotNull(copyRepository.AddedCopy);
        Assert.Equal(result.Id, copyRepository.AddedCopy.Id);
        Assert.Equal(edition.Id, result.BookEditionId);
        Assert.Equal(library.Id, result.LibraryId);
        Assert.Equal(1, copyRepository.CopyCount);
    }

    [Fact]
    public async Task GetByIdAsync_returns_the_copy_only_from_the_requested_library()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId);
        var edition = CreateEdition();
        var copy = CreateCopy(library.Id, edition);
        var service = CreateService(
            library,
            edition,
            new StubBookCopyRepository([copy]),
            userId);

        var result = await service.GetByIdAsync(library.Id, copy.Id);

        Assert.Equal(copy.Id, result.Id);
        Assert.Equal(library.Id, result.LibraryId);
    }

    [Fact]
    public async Task GetByIdAsync_returns_not_found_when_copy_belongs_to_another_library()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId);
        var edition = CreateEdition();
        var copy = CreateCopy(Guid.NewGuid(), edition);
        var service = CreateService(
            library,
            edition,
            new StubBookCopyRepository([copy]),
            userId);

        await Assert.ThrowsAsync<BookCopyNotFoundException>(() =>
            service.GetByIdAsync(library.Id, copy.Id));
    }

    private static LibraryBookService CreateService(
        Library library,
        BookEdition edition,
        StubBookCopyRepository copyRepository,
        Guid userId,
        BookMetadata? lookupMetadata = null,
        IReadOnlyList<Book>? books = null) =>
        new(
            new StubLibraryRepository(library),
            copyRepository,
            new StubEditionRepository(edition),
            new StubBookRepository(books ?? []),
            new StubMetadataLookupService(edition, lookupMetadata),
            new StubUnitOfWork(),
            new StubCurrentUserContext(userId));

    private static Library CreateLibrary(
        Guid userId,
        LibraryMemberRole role = LibraryMemberRole.Viewer)
    {
        var library = new Library { Id = Guid.NewGuid(), Name = "My library" };
        library.Members.Add(new LibraryMember
        {
            LibraryId = library.Id,
            UserId = userId,
            Role = role,
            Library = library
        });
        return library;
    }

    private static BookEdition CreateEdition(Book? book = null)
    {
        book ??= new Book { Id = Guid.NewGuid(), Title = "Nineteen Eighty-Four" };
        return new BookEdition
        {
            Id = Guid.NewGuid(),
            BookId = book.Id,
            Book = book,
            Isbn13 = Isbn
        };
    }

    private static BookCopy CreateCopy(Guid libraryId, BookEdition edition) =>
        new()
        {
            Id = Guid.NewGuid(),
            LibraryId = libraryId,
            BookEditionId = edition.Id,
            BookEdition = edition,
            Format = BookFormat.Paperback,
            ReadingStatus = ReadingStatus.Unread
        };

    private static AddBookCopyDto CreateAddDto(Guid editionId) =>
        new(
            editionId,
            null,
            BookFormat.Paperback,
            ReadingStatus.Unread,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

    private sealed class StubCurrentUserContext(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId { get; } = userId;
    }

    private sealed class StubLibraryRepository(Library library)
        : UnsupportedRepository<Library>, ILibraryRepository
    {
        public new ValueTask<Library?> GetByIdAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromResult<Library?>(library.Id == libraryId ? library : null);

        public Task<Library?> GetForUpdateAsync(Guid libraryId, CancellationToken cancellationToken = default) =>
            Task.FromResult<Library?>(library.Id == libraryId ? library : null);
        public Task<Library?> GetByIdWithDetailsAsync(Guid libraryId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<PagedResult<LibraryListItemViewModel>> GetUserLibrariesAsync(
            Guid userId, PageRequest page, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubEditionRepository(BookEdition edition)
        : UnsupportedRepository<BookEdition>, IBookEditionRepository
    {
        public Task<BookEdition?> GetByIsbnWithDetailsAsync(string isbn, CancellationToken cancellationToken = default) =>
            Task.FromResult<BookEdition?>(isbn == Isbn ? edition : null);
        public Task<BookEdition?> GetByIdWithDetailsAsync(Guid editionId, CancellationToken cancellationToken = default) =>
            Task.FromResult<BookEdition?>(edition.Id == editionId ? edition : null);
        public Task<List<BookEdition>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubBookRepository(IReadOnlyList<Book> books)
        : UnsupportedRepository<Book>, IBookRepository
    {
        public Task<List<Book>> GetAllByTitleWithDetailsAsync(
            string title,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(books
                .Where(book => string.Equals(book.Title, title, StringComparison.OrdinalIgnoreCase))
                .ToList());

        public Task<Book?> GetByIdWithDetailsAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<Book?> GetForUpdateAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<List<Book>> GetAllByTitleForUpdateAsync(string title, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<List<Book>> GetAllByAuthorIdAsync(Guid authorId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubBookCopyRepository(List<BookCopy> copies)
        : UnsupportedRepository<BookCopy>, IBookCopyRepository
    {
        public Guid? LastLibraryId { get; private set; }
        public Guid? LastEditionId { get; private set; }
        public Guid? LastBookId { get; private set; }
        public BookCopy? AddedCopy { get; private set; }
        public int CopyCount => copies.Count;

        public new void Add(BookCopy entity)
        {
            AddedCopy = entity;
            copies.Add(entity);
        }

        public Task<BookCopy?> GetByIdWithDetailsAsync(
            Guid bookCopyId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BookCopy?>(copies.SingleOrDefault(copy => copy.Id == bookCopyId));

        public Task<List<BookCopy>> GetAllByLibraryIdAsync(
            Guid libraryId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<BookCopy>> GetAllByLibraryAndBookEditionIdAsync(
            Guid libraryId,
            Guid bookEditionId,
            CancellationToken cancellationToken = default)
        {
            LastLibraryId = libraryId;
            LastEditionId = bookEditionId;
            return Task.FromResult(copies);
        }

        public Task<List<BookCopy>> GetAllByLibraryAndBookIdAsync(
            Guid libraryId,
            Guid bookId,
            CancellationToken cancellationToken = default)
        {
            LastLibraryId = libraryId;
            LastBookId = bookId;
            return Task.FromResult(copies
                .Where(copy =>
                    copy.LibraryId == libraryId &&
                    copy.BookEdition.BookId == bookId)
                .ToList());
        }
    }

    private sealed class StubMetadataLookupService(
        BookEdition edition,
        BookMetadata? lookupMetadata)
        : IBookMetadataLookupService
    {
        public Task<BookMetadataLookupResult> PreviewByIsbnAsync(
            string isbn,
            CancellationToken cancellationToken = default)
        {
            if (lookupMetadata is null)
                throw new BookMetadataNotFoundException(isbn);

            return Task.FromResult(new BookMetadataLookupResult(
                false,
                null,
                null,
                lookupMetadata.Provenance.RequiresReview,
                null,
                lookupMetadata.Provenance.Sources ?? [],
                lookupMetadata));
        }

        public Task<BookMetadataLookupResult> ImportAsync(
            string isbn,
            BookMetadata? metadata,
            bool confirmWebMetadata,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<BookEdition> ResolveEditionForImportAsync(
            string isbn,
            BookMetadata? metadata,
            bool confirmWebMetadata,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(edition);
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
        public async Task<int> ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default)
        {
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
        public void Add(T entity) => throw new NotSupportedException();
        public void Update(T entity) => throw new NotSupportedException();
        public void Remove(T entity) => throw new NotSupportedException();
    }
}
