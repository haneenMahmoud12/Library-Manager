using PersonalLibrary.Domain.Common;

namespace PersonalLibrary.Domain.Catalog;

public class Book : AuditableEntity
{
    public string Title { get; set; } = null!;
    public string? OriginalTitle { get; set; }
    public string? Description { get; set; }
    public string? OriginalLanguageCode { get; set; }
    public int? FirstPublishedYear { get; set; }

    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = [];
    public virtual ICollection<BookEdition> Editions { get; set; } = [];
}
