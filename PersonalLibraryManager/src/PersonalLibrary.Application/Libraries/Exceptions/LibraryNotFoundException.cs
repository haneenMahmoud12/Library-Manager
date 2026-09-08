namespace PersonalLibrary.Application.Libraries.Exceptions;

public sealed class LibraryNotFoundException(Guid libraryId)
    : Exception($"Library '{libraryId}' was not found.")
{
    public Guid LibraryId { get; } = libraryId;
}
