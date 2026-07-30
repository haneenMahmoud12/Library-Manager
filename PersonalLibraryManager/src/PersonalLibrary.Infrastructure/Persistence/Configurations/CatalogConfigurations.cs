using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Infrastructure.Persistence.Configurations;

public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Title).HasMaxLength(500).IsRequired();
        builder.Property(x => x.OriginalTitle).HasMaxLength(500);
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.OriginalLanguageCode).HasMaxLength(10);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Title);
    }
}

public sealed class AuthorConfiguration : IEntityTypeConfiguration<Author>
{
    public void Configure(EntityTypeBuilder<Author> builder)
    {
        builder.ToTable("Authors");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Biography).HasColumnType("nvarchar(max)");
        builder.Property(x => x.BirthDate).HasColumnType("date");
        builder.Property(x => x.DeathDate).HasColumnType("date");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Name);
    }
}

public sealed class BookAuthorConfiguration : IEntityTypeConfiguration<BookAuthor>
{
    public void Configure(EntityTypeBuilder<BookAuthor> builder)
    {
        builder.ToTable("BookAuthors");
        builder.HasKey(x => new { x.BookId, x.AuthorId }); //prevents accidentally inserting the same relationship twice
        builder.Property(x => x.AuthorOrder).HasDefaultValue(1).IsRequired();
        builder.HasIndex(x => x.AuthorId);

        builder.HasOne(x => x.Book).WithMany(x => x.BookAuthors)
            .HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Author).WithMany(x => x.BookAuthors)
            .HasForeignKey(x => x.AuthorId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PublisherConfiguration : IEntityTypeConfiguration<Publisher>
{
    public void Configure(EntityTypeBuilder<Publisher> builder)
    {
        builder.ToTable("Publishers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(300).IsRequired();
        builder.Property(x => x.WebsiteUrl).HasMaxLength(1000);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Name);
    }
}

public sealed class BookEditionConfiguration : IEntityTypeConfiguration<BookEdition>
{
    public void Configure(EntityTypeBuilder<BookEdition> builder)
    {
        builder.ToTable("BookEditions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Isbn10).HasMaxLength(10);
        builder.Property(x => x.Isbn13).HasMaxLength(13);
        builder.Property(x => x.EditionName).HasMaxLength(200);
        builder.Property(x => x.LanguageCode).HasMaxLength(10);
        builder.Property(x => x.PublicationDate).HasColumnType("date");
        builder.Property(x => x.CoverImageUrl).HasMaxLength(1000);
        builder.Property(x => x.Description).HasColumnType("nvarchar(max)");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Isbn10).IsUnique();
        builder.HasIndex(x => x.Isbn13).IsUnique();
        builder.HasIndex(x => x.BookId);
        builder.HasIndex(x => x.PublisherId);

        builder.HasOne(x => x.Book).WithMany(x => x.Editions)
            .HasForeignKey(x => x.BookId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Publisher).WithMany(x => x.Editions)
            .HasForeignKey(x => x.PublisherId).OnDelete(DeleteBehavior.SetNull);
    }
}
