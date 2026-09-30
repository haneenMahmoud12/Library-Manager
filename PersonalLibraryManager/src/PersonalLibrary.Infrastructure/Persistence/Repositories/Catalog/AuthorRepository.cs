using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Domain.Catalog;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Infrastructure.Persistence.Repositories.Catalog
{
    internal sealed class AuthorRepository(ApplicationDbContext context)
        : Repository<Author>(context), IAuthorRepository
    {
    }
}
