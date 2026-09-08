using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Libraries.Queries.GetMyLibraries
{
    public sealed record LibraryLocationResult(
        Guid Id,
        DateTime CreatedAt,
        DateTime? UpdatedAt,
        Guid LibraryId,
        string Name,
        Guid? ParentLocationId);
}
