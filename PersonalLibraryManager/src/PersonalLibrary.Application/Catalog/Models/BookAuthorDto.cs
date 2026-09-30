using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Catalog.Models
{
    public sealed record BookAuthorDto(
    Guid AuthorId,
    int AuthorOrder);
}
