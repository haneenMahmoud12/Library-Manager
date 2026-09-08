namespace PersonalLibrary.Application.Libraries.Exceptions;

public sealed class LibraryAccessDeniedException(Guid libraryId, Guid userId)
    : Exception("The user does not have permission to modify this library.")
{
    public Guid LibraryId { get; } = libraryId;
    public Guid UserId { get; } = userId;
}
