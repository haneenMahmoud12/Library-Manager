using PersonalLibrary.Application.Common.Pagination;

namespace PersonalLibrary.Api.Pagination;

public static class ApiPage
{
    public static PagedResult<T> Create<T>(
        IReadOnlyList<T> items,
        PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(request);

        IEnumerable<T> query = items;
        var properties = typeof(T).GetProperties()
            .Where(property => property.CanRead)
            .ToArray();

        if (request.SearchText is not null)
        {
            var stringProperties = properties
                .Where(property => property.PropertyType == typeof(string))
                .ToArray();
            query = query.Where(item => stringProperties.Any(property =>
                property.GetValue(item) is string value &&
                value.Contains(request.SearchText, StringComparison.OrdinalIgnoreCase)));
        }

        var orderProperty = properties.SingleOrDefault(property =>
            property.Name.Equals(request.OrderBy, StringComparison.OrdinalIgnoreCase));
        if (orderProperty is null || !IsSortable(orderProperty.PropertyType))
            throw new ArgumentException(
                $"'{request.OrderBy}' is not a sortable property of {typeof(T).Name}.",
                nameof(request.OrderBy));

        query = request.OrderDirection == OrderDirection.Descending
            ? query.OrderByDescending(item => orderProperty.GetValue(item), ObjectComparer.Instance)
            : query.OrderBy(item => orderProperty.GetValue(item), ObjectComparer.Instance);

        var filteredItems = query.ToList();
        var offset = (long)request.Offset;
        var pageItems = offset >= filteredItems.Count
            ? []
            : filteredItems.Skip((int)offset).Take(request.PageSize).ToList();

        return new PagedResult<T>(
            pageItems,
            request.PageNumber,
            request.PageSize,
            filteredItems.Count);
    }

    private static bool IsSortable(Type type)
    {
        var underlyingType = Nullable.GetUnderlyingType(type) ?? type;
        return underlyingType.IsEnum || typeof(IComparable).IsAssignableFrom(underlyingType);
    }

    private sealed class ObjectComparer : IComparer<object?>
    {
        public static ObjectComparer Instance { get; } = new();

        public int Compare(object? left, object? right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left is null) return -1;
            if (right is null) return 1;
            return ((IComparable)left).CompareTo(right);
        }
    }
}
