namespace PersonalLibrary.Api.Contracts.Catalog;

public sealed class CreateAuthorRequest
{
    public required string Name { get; init; }
    public string? Biography { get; init; }
    public DateOnly? BirthDate { get; init; }
    public DateOnly? DeathDate { get; init; }
}

public sealed class UpdateAuthorRequest
{
    public string? Name { get; init; }
    public string? Biography { get; init; }
    public DateOnly? BirthDate { get; init; }
    public DateOnly? DeathDate { get; init; }
}
