using PersonalLibrary.Domain.Common;
using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Domain.Libraries;

public class Library : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public Guid CreatedByUserId { get; set; }
    public LibraryVisibility Visibility { get; set; } = LibraryVisibility.Private;

    public virtual ICollection<LibraryMember> Members { get; set; } = [];
    public virtual ICollection<LibraryLocation> Locations { get; set; } = [];
    public virtual ICollection<BookCopy> BookCopies { get; set; } = [];
}
