using PersonalLibrary.Domain.Common;

namespace PersonalLibrary.Domain.Libraries;

public class LibraryLocation : AuditableEntity
{
    public Guid LibraryId { get; set; }
    public string Name { get; set; } = null!;
    public Guid? ParentLocationId { get; set; }

    public virtual Library Library { get; set; } = null!;
    public virtual LibraryLocation? ParentLocation { get; set; }
    public virtual ICollection<LibraryLocation> ChildLocations { get; set; } = [];
    public virtual ICollection<BookCopy> BookCopies { get; set; } = [];
}
