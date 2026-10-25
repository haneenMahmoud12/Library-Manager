using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Domain.Catalog.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Libraries.Models
{
    public sealed record ImportBookToLibraryDto(
        BookMetadata? Metadata,
        bool ConfirmWebMetadata,
        Guid? LocationId,
        BookFormat Format,
        ReadingStatus ReadingStatus,
        BookCondition? Condition,
        DateOnly? AcquisitionDate,
        decimal? PurchasePrice,
        string? CurrencyCode,
        bool? IsSigned,
        bool? IsFavourite,
        string? PersonalNotes);
}
