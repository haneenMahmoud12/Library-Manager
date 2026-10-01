namespace PersonalLibrary.Application.Catalog.Models;

public sealed record CreateAuthorDto(
    string Name,
    string? Biography,
    DateOnly? BirthDate,
    DateOnly? DeathDate);

public sealed record UpdateAuthorDto(
    string? Name,
    string? Biography,
    DateOnly? BirthDate,
    DateOnly? DeathDate);

public sealed record AuthorViewModel(
    Guid Id,
    string Name,
    string? Biography,
    DateOnly? BirthDate,
    DateOnly? DeathDate,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
