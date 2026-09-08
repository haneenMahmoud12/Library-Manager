using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Libraries.Queries.GetMyLibraries;
using PersonalLibrary.Application.Libraries.Repositories;
using PersonalLibrary.Domain.Libraries;

namespace PersonalLibrary.UnitTests.Libraries;

public sealed class GetMyLibrariesHandlerTests
{
    [Fact]
    public async Task HandleAsync_passes_current_user_and_page_to_repository()
    {
        var userId = Guid.NewGuid();
        var page = new PageRequest(
            pageNumber: 2,
            pageSize: 10,
            searchText: "home",
            orderBy: "Name",
            orderDirection: OrderDirection.Descending);
        var expected = new PagedResult<GetMyLibrariesResult>([], 2, 10, 12);
        var repository = new StubLibraryRepository(expected);
        var handler = new GetMyLibrariesHandler(
            repository,
            new StubCurrentUserContext(userId));

        var result = await handler.HandleAsync(new GetMyLibrariesQuery(page));

        Assert.Same(expected, result);
        Assert.Equal(userId, repository.UserId);
        Assert.Same(page, repository.Page);
        Assert.Equal(1, repository.QueryCalls);
    }

    [Fact]
    public async Task HandleAsync_rejects_unknown_order_column_before_querying()
    {
        var repository = new StubLibraryRepository(
            new PagedResult<GetMyLibrariesResult>([], 1, 20, 0));
        var handler = new GetMyLibrariesHandler(
            repository,
            new StubCurrentUserContext(Guid.NewGuid()));

        await Assert.ThrowsAsync<LibraryValidationException>(() =>
            handler.HandleAsync(new GetMyLibrariesQuery(
                new PageRequest(orderBy: "Password"))));

        Assert.Equal(0, repository.QueryCalls);
    }

    [Fact]
    public async Task HandleAsync_requires_an_authenticated_user()
    {
        var repository = new StubLibraryRepository(
            new PagedResult<GetMyLibrariesResult>([], 1, 20, 0));
        var handler = new GetMyLibrariesHandler(
            repository,
            new StubCurrentUserContext(null));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            handler.HandleAsync(new GetMyLibrariesQuery(new PageRequest())));

        Assert.Equal(0, repository.QueryCalls);
    }

    private sealed class StubCurrentUserContext(Guid? userId) : ICurrentUserContext
    {
        public Guid? UserId { get; } = userId;
    }

    private sealed class StubLibraryRepository(
        PagedResult<GetMyLibrariesResult> result) : ILibraryRepository
    {
        public int QueryCalls { get; private set; }
        public Guid? UserId { get; private set; }
        public PageRequest? Page { get; private set; }

        public Task<PagedResult<GetMyLibrariesResult>> GetUserLibrariesAsync(
            Guid userId,
            PageRequest page,
            CancellationToken cancellationToken = default)
        {
            QueryCalls++;
            UserId = userId;
            Page = page;
            return Task.FromResult(result);
        }

        public Task<Library?> GetForUpdateAsync(
            Guid libraryId,
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

        public void Add(Library entity) => throw new NotSupportedException();
        public void Update(Library entity) => throw new NotSupportedException();
        public void Remove(Library entity) => throw new NotSupportedException();
    }
}
