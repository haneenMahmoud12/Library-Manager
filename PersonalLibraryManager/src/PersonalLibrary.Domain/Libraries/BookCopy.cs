using PersonalLibrary.Domain.Catalog;
using PersonalLibrary.Domain.Catalog.Enums;
using PersonalLibrary.Domain.Common;

namespace PersonalLibrary.Domain.Libraries;

public class BookCopy : AuditableEntity
{
    public Guid LibraryId { get; set; }
    public Guid BookEditionId { get; set; }
    public Guid? LocationId { get; set; }
    public Guid AddedByUserId { get; set; }
    public BookFormat? Format { get; set; }
    public ReadingStatus ReadingStatus { get; set; } = ReadingStatus.Unread;
    public BookCondition? Condition { get; set; }
    public DateOnly? AcquisitionDate { get; set; }
    public decimal? PurchasePrice { get; set; }
    public string? CurrencyCode { get; set; }
    public bool IsSigned { get; set; }
    public bool IsFavourite { get; set; }
    public string? PersonalNotes { get; set; }

    public virtual Library Library { get; set; } = null!;
    public virtual BookEdition BookEdition { get; set; } = null!;
    public virtual LibraryLocation? Location { get; set; }
}
