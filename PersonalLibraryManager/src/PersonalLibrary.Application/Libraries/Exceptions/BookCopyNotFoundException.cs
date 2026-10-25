namespace PersonalLibrary.Application.Libraries.Exceptions;

public sealed class BookCopyNotFoundException(Guid libraryId, Guid bookCopyId)
    : Exception($"Book copy '{bookCopyId}' was not found in library '{libraryId}'.")
{
    public Guid LibraryId { get; } = libraryId;
    public Guid BookCopyId { get; } = bookCopyId;
}
