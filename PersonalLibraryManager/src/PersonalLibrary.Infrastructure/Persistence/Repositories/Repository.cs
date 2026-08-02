using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Common;
using System.Linq.Expressions;
using System.Reflection;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories;

internal sealed class Repository<T>(ApplicationDbContext context)
    : IRepository<T> where T : AuditableEntity
{
    private readonly DbSet<T> _entities = context.Set<T>();

    public ValueTask<T?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        _entities.FindAsync([id], cancellationToken);

    public async Task<PagedResult<T>> GetPageAsync(
        PageRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var query = ApplySearch(_entities.AsNoTracking(), request.SearchText);
        var totalCount = await query.LongCountAsync(cancellationToken);
        var items = await ApplyOrdering(
                query,
                request.OrderBy,
                request.OrderDirection)
            .Skip(request.Offset)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(
            items,
            request.PageNumber,
            request.PageSize,
            totalCount);
    }

    public Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        _entities.AnyAsync(entity => entity.Id == id, cancellationToken);

    public void Add(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _entities.Add(entity);
    }

    public void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _entities.Update(entity);
    }

    public void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _entities.Remove(entity);
    }

    private static IQueryable<T> ApplySearch(
        IQueryable<T> query,
        string? searchText)
    {
        if (searchText is null)
            return query;

        var stringProperties = typeof(T)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(property =>
                property.PropertyType == typeof(string) &&
                property.CanRead);

        var entity = Expression.Parameter(typeof(T), "entity");
        var search = Expression.Constant(searchText);
        Expression? predicate = null;

        foreach (var propertyInfo in stringProperties)
        {
            var property = Expression.Property(entity, propertyInfo);
            var isNotNull = Expression.NotEqual(
                property,
                Expression.Constant(null, typeof(string)));
            var contains = Expression.Call(
                property,
                nameof(string.Contains),
                Type.EmptyTypes,
                search);
            var matches = Expression.AndAlso(isNotNull, contains);
            predicate = predicate is null
                ? matches
                : Expression.OrElse(predicate, matches);
        }

        return predicate is null
            ? query
            : query.Where(Expression.Lambda<Func<T, bool>>(predicate, entity));
    }

    private static IOrderedQueryable<T> ApplyOrdering(
        IQueryable<T> query,
        string orderBy,
        OrderDirection direction)
    {
        var property = typeof(T).GetProperty(
            orderBy,
            BindingFlags.Instance |
            BindingFlags.Public |
            BindingFlags.IgnoreCase);

        if (property is null || !property.CanRead || !IsSortable(property.PropertyType))
            throw new ArgumentException(
                $"'{orderBy}' is not a sortable property of {typeof(T).Name}.",
                nameof(orderBy));

        var entity = Expression.Parameter(typeof(T), "entity");
        var key = Expression.Property(entity, property);
        var keySelector = Expression.Lambda(key, entity);
        var methodName = direction == OrderDirection.Descending
            ? nameof(Queryable.OrderByDescending)
            : nameof(Queryable.OrderBy);

        var ordered = (IOrderedQueryable<T>)query.Provider.CreateQuery<T>(
            Expression.Call(
                typeof(Queryable),
                methodName,
                [typeof(T), property.PropertyType],
                query.Expression,
                Expression.Quote(keySelector)));

        return property.Name == nameof(AuditableEntity.Id)
            ? ordered
            : ordered.ThenBy(entity => entity.Id);
    }

    private static bool IsSortable(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.IsEnum ||
               typeof(IComparable).IsAssignableFrom(underlyingType);
    }
}