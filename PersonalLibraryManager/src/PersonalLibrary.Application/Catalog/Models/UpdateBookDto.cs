using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Catalog.Models
{
    public sealed record UpdateBookDto(string? Title, string? OriginalTitle, string? Description, string? OriginalLanguageCode, int? FirstPublishedYear, IReadOnlyCollection<BookAuthorDto> Authors);

}
