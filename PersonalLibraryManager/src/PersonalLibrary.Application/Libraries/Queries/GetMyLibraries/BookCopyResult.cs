using PersonalLibrary.Domain.Catalog.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Libraries.Queries.GetMyLibraries
{
    public sealed record BookCopyResult(
        Guid Id,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid LibraryId,
        Guid BookEditionId,
        Guid? LocationId,
        Guid AddedByUserId,
        BookFormat? Format,
        ReadingStatus ReadingStatus,
        BookCondition? Condition,
        DateOnly? AcquisitionDate,
        decimal? PurchasePrice,
        string? CurrencyCode,
        bool IsSigned,
        bool IsFavourite,
        string? PersonalNotes);
}
