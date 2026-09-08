using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Domain.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories
{
    internal sealed class CatalogRepository(ApplicationDbContext context)
        : Repository<Book>(context), ICatalogRepository
    {
    }
}
