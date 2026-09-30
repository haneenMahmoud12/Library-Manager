using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Domain.Catalog;
using PersonalLibrary.Domain.Libraries;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Libraries.Repositories
{
    public interface IBookCopyRepository : IRepository<BookCopy>
    {
    }
}
