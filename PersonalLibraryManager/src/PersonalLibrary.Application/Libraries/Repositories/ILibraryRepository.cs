using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Libraries.Queries.GetMyLibraries;
using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Libraries;

namespace PersonalLibrary.Application.Libraries.Repositories;

public interface ILibraryRepository : IRepository<Library>
{
    Task<Library?> GetForUpdateAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default);

    Task<PagedResult<GetMyLibrariesResult>> GetUserLibrariesAsync(
        Guid userId,
        PageRequest page,
        CancellationToken cancellationToken = default);
}
