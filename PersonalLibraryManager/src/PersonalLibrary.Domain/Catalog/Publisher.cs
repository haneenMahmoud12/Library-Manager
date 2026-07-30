using PersonalLibrary.Domain.Common;

namespace PersonalLibrary.Domain.Catalog;

public class Publisher : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? WebsiteUrl { get; set; }

    public virtual ICollection<BookEdition> Editions { get; set; } = [];
}
