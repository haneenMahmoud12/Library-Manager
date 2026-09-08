namespace PersonalLibrary.Application.Libraries.Exceptions;

public sealed class LibraryValidationException(
    string field,
    string message)
    : Exception("Library request failed validation.")
{
    public string Field { get; } = field;
    public string ValidationMessage { get; } = message;
}
