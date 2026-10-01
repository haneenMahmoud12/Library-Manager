using PersonalLibrary.Application.Catalog.Exceptions;

namespace PersonalLibrary.Application.Catalog.Metadata;

public static class IsbnNormalizer
{
    public static string Normalize(string? value)
    {
        var isbn = new string((value ?? string.Empty)
            .Where(character => character is not ('-' or ' '))
            .ToArray())
            .ToUpperInvariant();

        if (isbn.Length == 10 && IsValidIsbn10(isbn))
            return isbn;
        if (isbn.Length == 13 && IsValidIsbn13(isbn))
            return isbn;

        throw new CatalogValidationException(
            "Isbn",
            "ISBN must be a valid ISBN-10 or ISBN-13 value.");
    }

    public static bool TryNormalize(string? value, out string? normalized)
    {
        try
        {
            normalized = Normalize(value);
            return true;
        }
        catch (CatalogValidationException)
        {
            normalized = null;
            return false;
        }
    }

    private static bool IsValidIsbn10(string isbn)
    {
        if (!isbn[..9].All(char.IsDigit) || !(char.IsDigit(isbn[9]) || isbn[9] == 'X'))
            return false;

        var sum = 0;
        for (var index = 0; index < 10; index++)
        {
            var digit = isbn[index] == 'X' ? 10 : isbn[index] - '0';
            sum += digit * (10 - index);
        }
        return sum % 11 == 0;
    }

    private static bool IsValidIsbn13(string isbn)
    {
        if (!isbn.All(char.IsDigit))
            return false;

        var sum = 0;
        for (var index = 0; index < 12; index++)
            sum += (isbn[index] - '0') * (index % 2 == 0 ? 1 : 3);

        var checkDigit = (10 - sum % 10) % 10;
        return checkDigit == isbn[12] - '0';
    }
}
