namespace PersonalLibrary.Application.Libraries.Commands.UpdateLibrary;

public sealed record UpdateLibraryCommand(
    Guid LibraryId,
    string? Name,
    string? Description,
    string? Visibility = null);
