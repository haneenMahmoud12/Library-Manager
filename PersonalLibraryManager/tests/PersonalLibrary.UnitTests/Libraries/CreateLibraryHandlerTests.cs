using System.Data;
using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Application.Libraries.Services;
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
        var service = new LibraryService(
            repository,
            unitOfWork,
            new StubCurrentUserContext(userId));

        var result = await service.SaveAsync(
            new CreateLibraryDto("  Home Library  ", "  My books  ", null));

        var library = Assert.IsType<Library>(repository.Added);
        Assert.Equal(result.Id, library.Id);
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
        var service = new LibraryService(
            repository,
            new StubUnitOfWork(),
            new StubCurrentUserContext(Guid.NewGuid()));

        var result = await service.SaveAsync(
            new CreateLibraryDto("Shared", null, "MembersOnly"));

        Assert.Equal(LibraryVisibility.MembersOnly, result.Visibility);
        Assert.Equal(LibraryVisibility.MembersOnly, repository.Added!.Visibility);
    }

    [Fact]
    public async Task HandleAsync_rejects_invalid_input_before_persisting()
    {
        var repository = new StubLibraryRepository();
        var unitOfWork = new StubUnitOfWork();
        var service = new LibraryService(
            repository,
            unitOfWork,
            new StubCurrentUserContext(Guid.NewGuid()));

        await Assert.ThrowsAsync<LibraryValidationException>(() =>
            service.SaveAsync(new CreateLibraryDto(" ", null, null)));

        Assert.Null(repository.Added);
        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task HandleAsync_requires_an_authenticated_user()
    {
        var service = new LibraryService(
            new StubLibraryRepository(),
            new StubUnitOfWork(),
            new StubCurrentUserContext(null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.SaveAsync(new CreateLibraryDto("Home", null, null)));
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

        public Task<Library?> GetByIdWithDetailsAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<PagedResult<LibraryListItemViewModel>> GetUserLibrariesAsync(
            Guid userId,
            PageRequest page,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public ValueTask<Library?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();

        public Task<List<Library>> GetAllAsync(
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
