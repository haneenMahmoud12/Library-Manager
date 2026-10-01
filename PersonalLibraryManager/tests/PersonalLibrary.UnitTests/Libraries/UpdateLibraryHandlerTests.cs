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

public sealed class UpdateLibraryHandlerTests
{
    [Theory]
    [InlineData(LibraryMemberRole.Owner)]
    [InlineData(LibraryMemberRole.Editor)]
    public async Task HandleAsync_allows_owner_and_editor(LibraryMemberRole role)
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId, role);
        var unitOfWork = new StubUnitOfWork();
        var service = new LibraryService(
            new StubLibraryRepository(library),
            unitOfWork,
            new StubCurrentUserContext(userId));

        var result = await service.SaveAsync(
            library.Id,
            new UpdateLibraryDto(
            "  Updated Library  ",
            "  Updated description  ",
            "Public"));

        Assert.Equal("Updated Library", result.Name);
        Assert.Equal("Updated description", result.Description);
        Assert.Equal(LibraryVisibility.Public, result.Visibility);
        Assert.NotNull(library.UpdatedAt);
        Assert.Equal(1, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task HandleAsync_updates_only_the_supplied_name()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId, LibraryMemberRole.Owner);
        library.Visibility = LibraryVisibility.FriendsOnly;
        library.Description = "Old description";
        var service = new LibraryService(
            new StubLibraryRepository(library),
            new StubUnitOfWork(),
            new StubCurrentUserContext(userId));

        var result = await service.SaveAsync(
            library.Id, new UpdateLibraryDto("Updated", null, null));

        Assert.Equal("Old description", result.Description);
        Assert.Equal(LibraryVisibility.FriendsOnly, result.Visibility);
    }

    [Fact]
    public async Task HandleAsync_clears_description_when_empty_string_is_supplied()
    {
        var userId = Guid.NewGuid();
        var library = CreateLibrary(userId, LibraryMemberRole.Owner);
        library.Description = "Old description";
        var service = new LibraryService(
            new StubLibraryRepository(library),
            new StubUnitOfWork(),
            new StubCurrentUserContext(userId));

        var result = await service.SaveAsync(
            library.Id, new UpdateLibraryDto(null, "", null));

        Assert.Null(result.Description);
        Assert.Equal("Original", result.Name);
    }

    [Fact]
    public async Task HandleAsync_rejects_a_request_with_no_fields()
    {
        var repository = new StubLibraryRepository(null);
        var service = new LibraryService(
            repository,
            new StubUnitOfWork(),
            new StubCurrentUserContext(Guid.NewGuid()));

        await Assert.ThrowsAsync<LibraryValidationException>(() =>
            service.SaveAsync(
                Guid.NewGuid(),
                new UpdateLibraryDto(null, null, null)));

        Assert.Equal(0, repository.LoadCalls);
    }

    [Fact]
    public async Task HandleAsync_denies_viewer_without_saving()
    {
        var userId = Guid.NewGuid();
        var unitOfWork = new StubUnitOfWork();
        var service = new LibraryService(
            new StubLibraryRepository(CreateLibrary(userId, LibraryMemberRole.Viewer)),
            unitOfWork,
            new StubCurrentUserContext(userId));

        await Assert.ThrowsAsync<LibraryAccessDeniedException>(() =>
            service.SaveAsync(Guid.NewGuid(), new UpdateLibraryDto("Updated", null, null)));

        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task HandleAsync_returns_not_found_without_saving()
    {
        var unitOfWork = new StubUnitOfWork();
        var service = new LibraryService(
            new StubLibraryRepository(null),
            unitOfWork,
            new StubCurrentUserContext(Guid.NewGuid()));

        await Assert.ThrowsAsync<LibraryNotFoundException>(() =>
            service.SaveAsync(Guid.NewGuid(), new UpdateLibraryDto("Updated", null, null)));

        Assert.Equal(0, unitOfWork.SaveCalls);
    }

    [Fact]
    public async Task HandleAsync_rejects_invalid_visibility_before_loading()
    {
        var repository = new StubLibraryRepository(null);
        var service = new LibraryService(
            repository,
            new StubUnitOfWork(),
            new StubCurrentUserContext(Guid.NewGuid()));

        await Assert.ThrowsAsync<LibraryValidationException>(() =>
            service.SaveAsync(
                Guid.NewGuid(),
                new UpdateLibraryDto("Updated", null, "Hidden")));

        Assert.Equal(0, repository.LoadCalls);
    }

    private static Library CreateLibrary(Guid userId, LibraryMemberRole role)
    {
        var library = new Library
        {
            Id = Guid.NewGuid(),
            Name = "Original",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };
        library.Members.Add(new LibraryMember
        {
            LibraryId = library.Id,
            UserId = userId,
            Role = role,
            JoinedAt = DateTimeOffset.UtcNow,
            Library = library
        });
        return library;
    }

    private sealed class StubCurrentUserContext(Guid? userId) : ICurrentUserContext
    {
        public Guid? UserId { get; } = userId;
    }

    private sealed class StubLibraryRepository(Library? library) : ILibraryRepository
    {
        public int LoadCalls { get; private set; }

        public Task<Library?> GetForUpdateAsync(
            Guid libraryId,
            CancellationToken cancellationToken = default)
        {
            LoadCalls++;
            return Task.FromResult(library);
        }

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

        public void Add(Library entity) => throw new NotSupportedException();
        public void Update(Library entity) => throw new NotSupportedException();
        public void Remove(Library entity) => throw new NotSupportedException();
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveCalls { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCalls++;
            return Task.FromResult(1);
        }

        public Task<int> ExecuteInTransactionAsync(
            Func<CancellationToken, Task> operation,
            IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
            CancellationToken cancellationToken = default) =>
            throw new NotSupportedException();
    }
}
