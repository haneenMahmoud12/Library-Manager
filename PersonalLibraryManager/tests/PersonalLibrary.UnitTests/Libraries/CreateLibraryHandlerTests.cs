using System.Data;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Commands.CreateLibrary;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Libraries.Queries.GetMyLibraries;
using PersonalLibrary.Application.Libraries.Repositories;
using PersonalLibrary.Application.Persistence;
using PersonalLibrary.Domain.Libraries;
using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.UnitTests.Libraries;

public sealed class CreateLibraryHandlerTests
{
    [Fact]
    public async Task HandleAsync_creates_library_and_owner_membership()
    {
        var userId = Guid.NewGuid();
        var repository = new StubLibraryRepository();
        var unitOfWork = new StubUnitOfWork();
        var handler = new CreateLibraryHandler(
            repository,
            unitOfWork,
            new StubCurrentUserContext(userId));

        var result = await handler.HandleAsync(
            new CreateLibraryCommand("  Home Library  ", "  My books  "));

        var library = Assert.IsType<Library>(repository.Added);
        Assert.Equal(result.LibraryId, library.Id);
        Assert.Equal("Home Library", library.Name);
        Assert.Equal("My books", library.Description);
        Assert.Equal(LibraryVisibility.Private, library.Visibility);
        Assert.Equal(userId, library.CreatedByUserId);

        var owner = Assert.Single(library.Members);
        Assert.Equal(userId, owner.UserId);
        Assert.Equal(LibraryMemberRole.Owner, owner.Role);
        Assert.Equal(library.Id, owner.LibraryId);
        Assert.Same(library, owner.Library);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task HandleAsync_accepts_a_named_visibility()
    {
        var repository = new StubLibraryRepository();
        var handler = new CreateLibraryHandler(
            repository,
            new StubUnitOfWork(),
            new StubCurrentUserContext(Guid.NewGuid()));

        var result = await handler.HandleAsync(
            new CreateLibraryCommand("Shared", null, "MembersOnly"));

        Assert.Equal(LibraryVisibility.MembersOnly, result.Visibility);
        Assert.Equal(LibraryVisibility.MembersOnly, repository.Added!.Visibility);
    }

    [Fact]
    public async Task HandleAsync_rejects_invalid_input_before_persisting()
    {
        var repository = new StubLibraryRepository();
        var unitOfWork = new StubUnitOfWork();
        var handler = new CreateLibraryHandler(
            repository,
            unitOfWork,
            new StubCurrentUserContext(Guid.NewGuid()));

        await Assert.ThrowsAsync<LibraryValidationException>(() =>
            handler.HandleAsync(new CreateLibraryCommand(" ", null)));

        Assert.Null(repository.Added);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task HandleAsync_requires_an_authenticated_user()
    {
        var handler = new CreateLibraryHandler(
            new StubLibraryRepository(),
            new StubUnitOfWork(),
            new StubCurrentUserContext(null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.HandleAsync(new CreateLibraryCommand("Home", null)));
    }

    private sealed class StubCurrentUserContext(Guid? userId) : ICurrentUserContext
    {
        public Guid? UserId { get; } = userId;
    }

    private sealed class StubLibraryRepository : ILibraryRepository
    {
        public Library? Added { get; private set; }

        public void Add(Library entity) => Added = entity;

        public Task<Library?> GetForUpdateAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedResult<GetMyLibrariesResult>> GetUserLibrariesAsync(
            Guid userId,
            PageRequest page,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<Library?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedResult<Library>> GetPageAsync(
            PageRequest request,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<bool> ExistsAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public void Update(Library entity) => throw new NotSupportedException();
        public void Remove(Library entity) => throw new NotSupportedException();
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(2);
        }

        public Task<int> ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
