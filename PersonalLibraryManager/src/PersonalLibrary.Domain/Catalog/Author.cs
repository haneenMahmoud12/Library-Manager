using PersonalLibrary.Domain.Common;

namespace PersonalLibrary.Domain.Catalog;

public class Author : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Biography { get; set; }
    public DateOnly? BirthDate { get; set; }
    public DateOnly? DeathDate { get; set; }

    public virtual ICollection<BookAuthor> BookAuthors { get; set; } = [];
}
