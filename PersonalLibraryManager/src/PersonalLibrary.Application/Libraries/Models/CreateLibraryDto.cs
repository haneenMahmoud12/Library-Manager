namespace PersonalLibrary.Application.Libraries.Models;

public sealed record CreateLibraryDto(string Name, string? Description, string? Visibility);
