using PersonalLibrary.Application.Catalog.Metadata;

namespace PersonalLibrary.Api.Contracts.Catalog;

public sealed class ImportBookMetadataRequest
{
    public required BookMetadata Metadata { get; init; }
    public bool ConfirmWebMetadata { get; init; }
}
