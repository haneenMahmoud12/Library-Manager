using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Common.Pagination;
using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Libraries.Models;
using PersonalLibrary.Domain.Libraries;
using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Application.Libraries.Services;

public sealed class LibraryService(
    ILibraryRepository libraryRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser) : ILibraryService
{
    private static readonly HashSet<string> AllowedOrderColumns =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "Id", "Name", "Visibility", "CurrentUserRole",
            "MemberCount", "BookCount", "CreatedAt", "UpdatedAt"
        };

    public async Task<LibraryViewModel> SaveAsync(
        CreateLibraryDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);

        var name = ValidateName(dto.Name);
        var description = ValidateDescription(dto.Description);
        var visibility = ParseVisibility(dto.Visibility, LibraryVisibility.Private);
        var userId = GetRequiredUserId();
        var now = DateTimeOffset.UtcNow;

        var library = new Library
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedByUserId = userId,
            Visibility = visibility,
            CreatedAt = now.UtcDateTime
        };

        library.Members.Add(new LibraryMember
        {
            LibraryId = library.Id,
            UserId = userId,
            Role = LibraryMemberRole.Owner,
            JoinedAt = now,
            Library = library
        });

        libraryRepository.Add(library);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(library, LibraryMemberRole.Owner);
    }

    public async Task<LibraryViewModel> SaveAsync(
        Guid libraryId,
        UpdateLibraryDto dto,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateLibraryId(libraryId);

        if (dto.Name is null && dto.Description is null && dto.Visibility is null)
            throw new LibraryValidationException("Request", "At least one field must be supplied.");

        var name = dto.Name is null ? null : ValidateName(dto.Name);
        var description = dto.Description is null ? null : ValidateDescription(dto.Description);
        LibraryVisibility? visibility = dto.Visibility is null
            ? null
            : ParseVisibility(dto.Visibility);
        var userId = GetRequiredUserId();
        var library = await libraryRepository.GetForUpdateAsync(libraryId, cancellationToken)
            ?? throw new LibraryNotFoundException(libraryId);
        var membership = GetAuthorizedMembership(library, userId, requireWriteAccess: true);

        if (dto.Name is not null)
            library.Name = name!;
        if (dto.Description is not null)
            library.Description = description;
        if (visibility.HasValue)
            library.Visibility = visibility.Value;

        library.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Map(library, membership.Role);
    }

    public async Task<LibraryViewModel> GetByIdAsync(
        Guid libraryId,
        CancellationToken cancellationToken = default)
    {
        ValidateLibraryId(libraryId);
        var userId = GetRequiredUserId();
        var library = await libraryRepository.GetByIdWithDetailsAsync(libraryId, cancellationToken)
            ?? throw new LibraryNotFoundException(libraryId);
        var membership = GetAuthorizedMembership(library, userId, requireWriteAccess: false);
        return Map(library, membership.Role);
    }

    public Task<PagedResult<LibraryListItemViewModel>> GetMyLibrariesAsync(
        PageRequest page,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(page);
        var userId = GetRequiredUserId();

        if (!AllowedOrderColumns.Contains(page.OrderBy))
        {
            throw new LibraryValidationException(
                "OrderBy",
                $"OrderBy must be one of: {string.Join(", ", AllowedOrderColumns)}.");
        }

        return libraryRepository.GetUserLibrariesAsync(userId, page, cancellationToken);
    }

    private Guid GetRequiredUserId() =>
        currentUser.UserId is { } userId && userId != Guid.Empty
            ? userId
            : throw new UnauthorizedAccessException("An authenticated user is required.");

    private static LibraryMember GetAuthorizedMembership(
        Library library,
        Guid userId,
        bool requireWriteAccess)
    {
        var membership = library.Members.SingleOrDefault(member => member.UserId == userId)
            ?? throw new LibraryAccessDeniedException(library.Id, userId);

        if (requireWriteAccess &&
            membership.Role is not (LibraryMemberRole.Owner or LibraryMemberRole.Editor))
        {
            throw new LibraryAccessDeniedException(library.Id, userId);
        }

        return membership;
    }

    private static string ValidateName(string? value)
    {
        var name = value?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            throw new LibraryValidationException("Name", "Library name is required and cannot exceed 200 characters.");
        return name;
    }

    private static string? ValidateDescription(string? value)
    {
        var description = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (description?.Length > 1000)
            throw new LibraryValidationException("Description", "Library description cannot exceed 1000 characters.");
        return description;
    }

    private static LibraryVisibility ParseVisibility(
        string? value,
        LibraryVisibility? defaultValue = null)
    {
        if (value is null && defaultValue.HasValue)
            return defaultValue.Value;

        if (!string.IsNullOrWhiteSpace(value) &&
            Enum.TryParse<LibraryVisibility>(value.Trim(), true, out var visibility) &&
            Enum.IsDefined(visibility))
        {
            return visibility;
        }

        throw new LibraryValidationException(
            "Visibility",
            "Visibility must be Private, MembersOnly, FriendsOnly, or Public.");
    }

    private static void ValidateLibraryId(Guid libraryId)
    {
        if (libraryId == Guid.Empty)
            throw new LibraryValidationException("LibraryId", "A valid library ID is required.");
    }

    private static LibraryViewModel Map(Library library, LibraryMemberRole role) =>
        new(
            library.Id,
            library.Name,
            library.Description,
            library.Visibility,
            role,
            library.Members.Count,
            library.BookCopies.Count,
            library.CreatedAt,
            library.UpdatedAt);
}
