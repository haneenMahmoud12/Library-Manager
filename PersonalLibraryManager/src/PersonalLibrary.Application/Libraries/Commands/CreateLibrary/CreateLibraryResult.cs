using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Application.Libraries.Commands.CreateLibrary;

public sealed record CreateLibraryResult(
    Guid LibraryId,
    string Name,
    string? Description,
    LibraryVisibility Visibility);
