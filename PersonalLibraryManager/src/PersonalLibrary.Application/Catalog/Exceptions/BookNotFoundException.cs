using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Catalog.Exceptions
{
    public sealed class BookNotFoundException(Guid bookId)
    : Exception($"Book '{bookId}' was not found.")
    {
        public Guid BookId { get; } = bookId;
    }

}
