namespace PersonalLibrary.Application.Catalog.Exceptions;

public sealed class CatalogValidationException(string field, string validationMessage)
    : Exception(validationMessage)
{
    public string Field { get; } = field;
    public string ValidationMessage { get; } = validationMessage;
}
