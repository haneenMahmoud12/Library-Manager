using PersonalLibrary.Application.Common.Authentication;
using PersonalLibrary.Application.Libraries.Exceptions;
using PersonalLibrary.Application.Libraries.Repositories;
using PersonalLibrary.Application.Persistence;
using PersonalLibrary.Domain.Libraries;
using PersonalLibrary.Domain.Libraries.Enums;

namespace PersonalLibrary.Application.Libraries.Commands.CreateLibrary;

public sealed class CreateLibraryHandler(
    ILibraryRepository libraryRepository,
    IUnitOfWork unitOfWork,
    ICurrentUserContext currentUser)
{
    public async Task<CreateLibraryResult> HandleAsync(
        CreateLibraryCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);

        var name = command.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name) || name.Length > 200)
        {
            throw new LibraryValidationException(
                "Name",
                "Library name is required and cannot exceed 200 characters.");
        }

        var description = string.IsNullOrWhiteSpace(command.Description)
            ? null
            : command.Description.Trim();
        if (description?.Length > 1000)
        {
            throw new LibraryValidationException(
                "Description",
                "Library description cannot exceed 1000 characters.");
        }

        var visibility = ParseVisibility(command.Visibility);
        var userId = currentUser.UserId;
        if (userId is null || userId == Guid.Empty)
            throw new UnauthorizedAccessException("An authenticated user is required.");

        var now = DateTimeOffset.UtcNow;
        var library = new Library
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = description,
            CreatedByUserId = userId.Value,
            Visibility = visibility,
            CreatedAt = now.UtcDateTime
        };

        library.Members.Add(new LibraryMember
        {
            LibraryId = library.Id,
            UserId = userId.Value,
            Role = LibraryMemberRole.Owner,
            JoinedAt = now,
            Library = library
        });

        libraryRepository.Add(library);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateLibraryResult(
            library.Id,
            library.Name,
            library.Description,
            library.Visibility);
    }

    private static LibraryVisibility ParseVisibility(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return LibraryVisibility.Private;

        if (Enum.TryParse<LibraryVisibility>(value.Trim(), true, out var visibility) &&
            Enum.IsDefined(visibility))
        {
            return visibility;
        }

        throw new LibraryValidationException(
            "Visibility",
            "Visibility must be Private, MembersOnly, FriendsOnly, or Public.");
    }
}
