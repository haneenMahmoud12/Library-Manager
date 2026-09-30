using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Application.Libraries.Models;

public sealed record LibraryListItemViewModel(
    Guid Id,
    string Name,
    string? Description,
    LibraryVisibility Visibility,
    LibraryMemberRole CurrentUserRole,
    int MemberCount,
    int BookCount,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
