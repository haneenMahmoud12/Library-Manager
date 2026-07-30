namespace PersonalLibrary.Domain.Catalog;

public class BookAuthor
{
    public Guid BookId { get; set; }
    public Guid AuthorId { get; set; }
    public int AuthorOrder { get; set; } = 1;

    public virtual Book Book { get; set; } = null!;
    public virtual Author Author { get; set; } = null!;
}
