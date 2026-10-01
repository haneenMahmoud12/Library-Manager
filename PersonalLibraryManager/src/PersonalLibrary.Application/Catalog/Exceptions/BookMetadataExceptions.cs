namespace PersonalLibrary.Application.Catalog.Exceptions;

public sealed class BookMetadataNotFoundException(string isbn)
    : Exception($"No metadata was found for ISBN '{isbn}'.")
{
    public string Isbn { get; } = isbn;
}

public sealed class BookMetadataProviderException(string message, Exception? innerException = null)
    : Exception(message, innerException);
