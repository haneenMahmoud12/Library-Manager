using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Libraries.Repositories;
using PersonalLibrary.Application.Persistence;
using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Application.Libraries.Commands.UpdateLibrary;

public sealed class UpdateLibraryHandler(
    ILibraryRepository libraryRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser)
{
    public async Task<UpdateLibraryResult> HandleAsync(
        UpdateLibraryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (command.LibraryId == Guid.Empty)
        {
            throw new LibraryValidationException(
                "LibraryId",
                "A valid library ID is required.");
        }

        if (command.Name is null &&
            command.Description is null &&
            command.Visibility is null)
        {
            throw new LibraryValidationException(
                "Request",
                "At least one field must be supplied.");
        }

        string? name = null;
        if (command.Name is not null)
        {
            name = command.Name.Trim();
            if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
            {
                throw new LibraryValidationException(
                    "Name",
                    "Library name is required and cannot exceed 200 characters.");
            }
        }

        string? description = null;
        if (command.Description is not null)
        {
            description = string.IsNullOrWhiteSpace(command.Description)
                ? null
                : command.Description.Trim();
            if (description?.Length > 1000)
            {
                throw new LibraryValidationException(
                    "Description",
                    "Library description cannot exceed 1000 characters.");
            }
        }

        var visibility = ParseVisibility(command.Visibility);
        var userId = currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            throw new UnauthorizedAccessException("An authenticated user is required.");

        var library = await libraryRepository.GetForUpdateAsync(
            command.LibraryId,
            cancellationToken);
        if (library is null)
            throw new LibraryNotFoundException(command.LibraryId);

        var membership = library.Members.SingleOrDefault(member => member.UserId == userId.Value);
        if (membership?.Role is not (LibraryMemberRole.Owner or LibraryMemberRole.Editor))
            throw new LibraryAccessDeniedException(command.LibraryId, userId.Value);

        if (command.Name is not null)
            library.Name = name!;
        if (command.Description is not null)
            library.Description = description;
        if (visibility is not null)
            library.Visibility = visibility.Value;
        library.UpdatedAt = DateTime.UtcNow;

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateLibraryResult(
            library.Id,
            library.Name,
            library.Description,
            library.Visibility,
            library.UpdatedAt.Value);
    }

    private static LibraryVisibility? ParseVisibility(string? value)
    {
        if (value is null)
            return null;

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
}
