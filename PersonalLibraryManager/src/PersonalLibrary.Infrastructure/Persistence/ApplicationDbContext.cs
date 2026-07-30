using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Domain.Catalog;
using PersonalLibrary.Domain.Libraries;
using PersonalLibrary.Domain.Loans;
using PersonalLibrary.Domain.Social;
using PersonalLibrary.Infrastructure.Identity;

namespace PersonalLibrary.Infrastructure.Persistence
{
    public sealed class ApplicationDbContext
    : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Book> Books => Set<Book>();
        public DbSet<BookEdition> BookEditions => Set<BookEdition>();
        public DbSet<BookCopy> BookCopies => Set<BookCopy>();
        public DbSet<Author> Authors => Set<Author>();
        public DbSet<BookAuthor> BookAuthors => Set<BookAuthor>();
        public DbSet<Publisher> Publishers => Set<Publisher>();
        public DbSet<Library> Libraries => Set<Library>();
        public DbSet<LibraryMember> LibraryMembers => Set<LibraryMember>();
        public DbSet<LibraryLocation> LibraryLocations => Set<LibraryLocation>();
        public DbSet<Loan> Loans => Set<Loan>();
        public DbSet<UserFriend> UserFriends => Set<UserFriend>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.ApplyConfigurationsFromAssembly(
                typeof(ApplicationDbContext).Assembly);
        }
    }
}
