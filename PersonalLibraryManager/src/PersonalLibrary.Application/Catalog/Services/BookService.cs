using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Application.Common.Authentication;
using System;
using System.Collections.Generic;
using System.Text;

namespace PersonalLibrary.Application.Catalog.Services
{
    public class BookService(
        IBookRepository bookRepository, 
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser) : IBookService
    {
    }
}
