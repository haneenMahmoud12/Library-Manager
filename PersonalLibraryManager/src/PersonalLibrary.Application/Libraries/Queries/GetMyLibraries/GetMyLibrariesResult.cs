using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Application.Libraries.Queries.GetMyLibraries;

public sealed record GetMyLibrariesResult(
    Guid Id,
    string Name,
    string? Description,
    LibraryVisibility Visibility,
    LibraryMemberRole CurrentUserRole,
    int MemberCount,
    int BookCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
