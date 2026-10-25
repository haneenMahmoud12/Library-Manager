using PersonalLibrary.Application.Common.Pagination;

namespace PersonalLibrary.Api.Pagination;

public static class ApiPage
{
    public static PagedResult<T> Create<T>(
        IReadOnlyList<T> items,
        int pageNumber,
        int pageSize)
    {
        _ = new PageRequest(pageNumber, pageSize);

        var offset = (long)(pageNumber - 1) * pageSize;
        var pageItems = offset >= items.Count
            ? []
            : items.Skip((int)offset).Take(pageSize).ToList();

        return new PagedResult<T>(pageItems, pageNumber, pageSize, items.Count);
    }
}
