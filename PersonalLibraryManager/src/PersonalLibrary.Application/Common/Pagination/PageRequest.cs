namespace PersonalLibrary.Application.Common.Pagination;

public sealed record PageRequest
{
    public const int DefaultPageSize = 20;
    public const int MaximumPageSize = 100;
    public const int MaximumSearchLength = 200;

    public PageRequest(
        int pageNumber = 1,
        int pageSize = DefaultPageSize,
        string? searchText = null,
        string orderBy = "Id",
        OrderDirection orderDirection = OrderDirection.Ascending)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(pageSize, MaximumPageSize);
        ArgumentException.ThrowIfNullOrWhiteSpace(orderBy);

        if (searchText?.Length > MaximumSearchLength)
            throw new ArgumentException(
                $"Search text cannot exceed {MaximumSearchLength} characters.",
                nameof(searchText));

        if (orderBy.Length > 100)
            throw new ArgumentException(
                "Order-by column cannot exceed 100 characters.",
                nameof(orderBy));

        if (!Enum.IsDefined(orderDirection))
            throw new ArgumentOutOfRangeException(nameof(orderDirection));

        PageNumber = pageNumber;
        PageSize = pageSize;
        SearchText = string.IsNullOrWhiteSpace(searchText) ? null : searchText.Trim();
        OrderBy = orderBy.Trim();
        OrderDirection = orderDirection;
    }

    public int PageNumber { get; }
    public int PageSize { get; }
    public string? SearchText { get; }
    public string OrderBy { get; }
    public OrderDirection OrderDirection { get; }
    public int Offset => checked((PageNumber - 1) * PageSize);
}