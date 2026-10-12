using System.Data;
using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Repositories;
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
        Assert.Equal(2, result.CopyCount);
        Assert.Equal(edition.BookId, result.BookId);
        Assert.Equal(edition.Id, result.BookEditionId);
        Assert.Equal(2, result.Copies.Count);
        Assert.Equal(library.Id, copyRepository.LastLibraryId);
        Assert.Equal(edition.Id, copyRepository.LastEditionId);
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
        Assert.Equal(0, result.CopyCount);
        Assert.Equal(edition.Id, result.BookEditionId);
        Assert.Empty(result.Copies);
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

    private static LibraryBookService CreateService(
        Library library,
        BookEdition edition,
        StubBookCopyRepository copyRepository,
        Guid userId) =>
        new(
            new StubLibraryRepository(library),
            copyRepository,
            new StubEditionRepository(edition),
            new StubUnitOfWork(),
            new StubCurrentUserContext(userId));

    private static Library CreateLibrary(Guid userId)
    {
        var library = new Library { Id = Guid.NewGuid(), Name = "My library" };
        library.Members.Add(new LibraryMember
        {
            LibraryId = library.Id,
            UserId = userId,
            Role = LibraryMemberRole.Viewer,
            Library = library
        });
        return library;
    }

    private static BookEdition CreateEdition()
    {
        var book = new Book { Id = Guid.NewGuid(), Title = "Nineteen Eighty-Four" };
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

    private sealed class StubCurrentUserContext(Guid userId) : ICurrentUserContext
    {
        public Guid? UserId { get; } = userId;
    }

    private sealed class StubLibraryRepository(Library library)
        : UnsupportedRepository<Library>, ILibraryRepository
    {
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
            throw new NotSupportedException();
        public Task<List<BookEdition>> GetAllByBookIdAsync(Guid bookId, CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }

    private sealed class StubBookCopyRepository(List<BookCopy> copies)
        : UnsupportedRepository<BookCopy>, IBookCopyRepository
    {
        public Guid? LastLibraryId { get; private set; }
        public Guid? LastEditionId { get; private set; }

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
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
        public Task<int> ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
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
