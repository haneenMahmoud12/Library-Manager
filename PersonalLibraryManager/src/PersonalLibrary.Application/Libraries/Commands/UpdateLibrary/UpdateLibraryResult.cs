using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Application.Libraries.Commands.UpdateLibrary;

public sealed record UpdateLibraryResult(
    Guid LibraryId,
    string Name,
    string? Description,
    LibraryVisibility Visibility,
    DateTime UpdatedAtUtc);
