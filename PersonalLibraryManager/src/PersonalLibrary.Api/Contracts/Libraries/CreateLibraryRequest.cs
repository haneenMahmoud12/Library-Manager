using System.ComponentModel.DataAnnotations;

namespace PersonalLibrary.Api.Contracts.Libraries;

public sealed class CreateLibraryRequest
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public required string Name { get; init; }

    [StringLength(1000)]
    public string? Description { get; init; }

    public string? Visibility { get; init; }
}
