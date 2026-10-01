using PersonalLibrary.Domain.Catalog.Enums;

namespace PersonalLibrary.Api.Contracts.Catalog;

public sealed class AddBookManuallyRequest
{
    public Guid BookEditionId { get; init; }
    public Guid? LocationId { get; init; }
    public BookFormat Format { get; init; }
    public ReadingStatus ReadingStatus { get; init; } = ReadingStatus.Unread;
    public BookCondition? Condition { get; init; }
    public DateOnly? AcquisitionDate { get; init; }
    public decimal? PurchasePrice { get; init; }
    public string? CurrencyCode { get; init; }
    public bool? IsSigned { get; init; }
    public bool? IsFavourite { get; init; }
    public string? PersonalNotes { get; init; }
}
