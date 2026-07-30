using PersonalLibrary.Domain.Common;
using PersonalLibrary.Domain.Libraries;

namespace PersonalLibrary.Domain.Catalog;

public class BookEdition : AuditableEntity
{
    public Guid BookId { get; set; }
    public Guid? PublisherId { get; set; }
    public string? Isbn10 { get; set; }
    public string? Isbn13 { get; set; }
    public string? EditionName { get; set; }
    public string? LanguageCode { get; set; }
    public DateOnly? PublicationDate { get; set; }
    public int? PageCount { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? Description { get; set; }

    public virtual Book Book { get; set; } = null!;
    public virtual Publisher? Publisher { get; set; }
    public virtual ICollection<BookCopy> BookCopies { get; set; } = [];
}
