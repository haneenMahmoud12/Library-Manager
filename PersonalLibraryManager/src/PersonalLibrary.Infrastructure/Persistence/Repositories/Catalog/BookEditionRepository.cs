using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Domain.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories.Catalog
{
    internal sealed class BookEditionRepository(ApplicationDbContext context)
        : Repository<BookEdition>(context), IBookEditionRepository
    {
    }
}
