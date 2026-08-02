using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Domain.Common;

namespace PersonalLibrary.Application.Persistence.IRepositories;

public interface IRepository<T> where T : AuditableEntity
{
    ValueTask<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<PagedResult<T>> GetPageAsync(
        PageRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    void Add(T entity);
    void Update(T entity);
    void Remove(T entity);
}