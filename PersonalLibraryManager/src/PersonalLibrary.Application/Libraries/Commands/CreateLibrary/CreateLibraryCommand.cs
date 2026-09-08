namespace PersonalLibrary.Application.Libraries.Commands.CreateLibrary;

public sealed record CreateLibraryCommand(
    string Name,
    string? Description,
    string? Visibility = null);
