using Microsoft.EntityFrameworkCore;
using PersonalLibrary.Domain.Catalog;

namespace PersonalLibrary.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    private static readonly DateTime SeededAt = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    public static async Task SeedCatalogAsync(
        ApplicationDbContext context,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var authors = await context.Authors.ToListAsync(cancellationToken);
        var publishers = await context.Publishers.ToListAsync(cancellationToken);
        var books = await context.Books.ToListAsync(cancellationToken);

        var authorMap = AddMissingAuthors(context, authors);
        var publisherMap = AddMissingPublishers(context, publishers);
        var bookMap = AddMissingBooks(context, books);

        var existingLinks = (await context.BookAuthors
                .Select(link => new { link.BookId, link.AuthorId })
                .ToListAsync(cancellationToken))
            .Select(link => (link.BookId, link.AuthorId))
            .ToHashSet();

        foreach (var link in SeedBookAuthors)
        {
            var bookId = bookMap[link.BookTitle].Id;
            var authorId = authorMap[link.AuthorName].Id;
            if (!existingLinks.Add((bookId, authorId)))
                continue;

            context.BookAuthors.Add(new BookAuthor
            {
                BookId = bookId,
                AuthorId = authorId,
                AuthorOrder = link.AuthorOrder
            });
        }

        var booksWithEditions = (await context.BookEditions
                .Select(edition => edition.BookId)
                .Distinct()
                .ToListAsync(cancellationToken))
            .ToHashSet();

        var editionNumber = 1;
        foreach (var definition in SeedBooks)
        {
            var book = bookMap[definition.Title];
            if (!booksWithEditions.Add(book.Id))
            {
                editionNumber++;
                continue;
            }

            var publisher = publisherMap[definition.PublisherName];
            context.BookEditions.Add(new BookEdition
            {
                Id = Guid.Parse($"D0000000-0000-4000-8000-{editionNumber:000000000000}"),
                BookId = book.Id,
                PublisherId = publisher.Id,
                EditionName = "Seeded print edition",
                LanguageCode = definition.LanguageCode,
                PublicationDate = definition.FirstPublishedYear is { } year
                    ? new DateOnly(year, 1, 1)
                    : null,
                Description = $"A seeded edition of {definition.Title}.",
                CreatedAt = SeededAt
            });
            editionNumber++;
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<string, Author> AddMissingAuthors(
        ApplicationDbContext context,
        List<Author> existing)
    {
        var result = new Dictionary<string, Author>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in SeedAuthors)
        {
            var author = existing.FirstOrDefault(item => item.Id == definition.Id)
                ?? existing.FirstOrDefault(item => item.Name == definition.Name);
            if (author is null)
            {
                author = new Author
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    Biography = definition.Biography,
                    BirthDate = definition.BirthDate,
                    DeathDate = definition.DeathDate,
                    CreatedAt = SeededAt
                };
                context.Authors.Add(author);
                existing.Add(author);
            }
            result[definition.Name] = author;
        }
        return result;
    }

    private static Dictionary<string, Publisher> AddMissingPublishers(
        ApplicationDbContext context,
        List<Publisher> existing)
    {
        var result = new Dictionary<string, Publisher>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in SeedPublishers)
        {
            var publisher = existing.FirstOrDefault(item => item.Id == definition.Id)
                ?? existing.FirstOrDefault(item => item.Name == definition.Name);
            if (publisher is null)
            {
                publisher = new Publisher
                {
                    Id = definition.Id,
                    Name = definition.Name,
                    WebsiteUrl = definition.WebsiteUrl,
                    CreatedAt = SeededAt
                };
                context.Publishers.Add(publisher);
                existing.Add(publisher);
            }
            result[definition.Name] = publisher;
        }
        return result;
    }

    private static Dictionary<string, Book> AddMissingBooks(
        ApplicationDbContext context,
        List<Book> existing)
    {
        var result = new Dictionary<string, Book>(StringComparer.OrdinalIgnoreCase);
        foreach (var definition in SeedBooks)
        {
            var book = existing.FirstOrDefault(item => item.Id == definition.Id)
                ?? existing.FirstOrDefault(item => item.Title == definition.Title);
            if (book is null)
            {
                book = new Book
                {
                    Id = definition.Id,
                    Title = definition.Title,
                    OriginalTitle = definition.OriginalTitle,
                    Description = definition.Description,
                    OriginalLanguageCode = definition.LanguageCode,
                    FirstPublishedYear = definition.FirstPublishedYear,
                    CreatedAt = SeededAt
                };
                context.Books.Add(book);
                existing.Add(book);
            }
            result[definition.Title] = book;
        }
        return result;
    }

    private static readonly AuthorSeed[] SeedAuthors =
    [
        new(Guid.Parse("117925A5-88DF-4054-90A1-D2F3C5BD32B4"), "Mirna Elmahdy", "Egyptian novelist known for the Noah Al-Alfi mystery series.", null, null),
        new(Guid.Parse("DC09B156-82F1-4CC3-B886-DE3170B9DB76"), "Dr. Mostafa Mahmoud", "Egyptian physician, philosopher, and writer.", new DateOnly(1921, 12, 27), new DateOnly(2009, 10, 31)),
        new(Guid.Parse("449189B0-A7CD-4172-8D32-F0A5824DCC16"), "Dr. Ahmad Khalid Tawfik", "Egyptian author and physician, widely regarded as a pioneer of Arabic genre fiction.", new DateOnly(1962, 6, 10), new DateOnly(2018, 4, 2)),
        new(Guid.Parse("A0000000-0000-4000-8000-000000000001"), "Naguib Mahfouz", "Egyptian novelist and recipient of the 1988 Nobel Prize in Literature.", new DateOnly(1911, 12, 11), new DateOnly(2006, 8, 30)),
        new(Guid.Parse("A0000000-0000-4000-8000-000000000002"), "Taha Hussein", "Egyptian writer and intellectual known as the Dean of Arabic Literature.", new DateOnly(1889, 11, 15), new DateOnly(1973, 10, 28)),
        new(Guid.Parse("A0000000-0000-4000-8000-000000000003"), "Agatha Christie", "English writer known for her detective novels featuring Hercule Poirot and Miss Marple.", new DateOnly(1890, 9, 15), new DateOnly(1976, 1, 12)),
        new(Guid.Parse("A0000000-0000-4000-8000-000000000004"), "George Orwell", "English novelist and essayist whose work examined totalitarianism and social injustice.", new DateOnly(1903, 6, 25), new DateOnly(1950, 1, 21))
    ];

    private static readonly PublisherSeed[] SeedPublishers =
    [
        new(Guid.Parse("B0000000-0000-4000-8000-000000000001"), "Dar Al Karma", "https://www.alkarmabooks.com"),
        new(Guid.Parse("B0000000-0000-4000-8000-000000000002"), "Kayan Publishing", null),
        new(Guid.Parse("B0000000-0000-4000-8000-000000000003"), "Dar Al Maaref", null),
        new(Guid.Parse("B0000000-0000-4000-8000-000000000004"), "Dar El Shorouk", "https://www.shorouk.com"),
        new(Guid.Parse("B0000000-0000-4000-8000-000000000005"), "HarperCollins", "https://www.harpercollins.com")
    ];

    private static readonly BookSeed[] SeedBooks =
    [
        new(Guid.Parse("23AAB23B-3E6C-4AB3-B26D-1153853FC2A6"), "زغازيغ", "زغازيغ", "A collection of articles and reflections by Ahmad Khalid Tawfik.", "ar", 2014, "Kayan Publishing"),
        new(Guid.Parse("C40E4D5E-91B4-4ECC-8A3B-204A3E0DCF34"), "يوتوبيا", "يوتوبيا", "A dystopian novel set in a deeply divided future Egypt.", "ar", 2008, "Kayan Publishing"),
        new(Guid.Parse("50895FA2-95F2-4D14-9D5F-3A0D07661903"), "تحقيقات نوح الألفي: قضية ست الحسن", "تحقيقات نوح الألفي: قضية ست الحسن", "The first Noah Al-Alfi mystery novel.", "ar", 2018, "Dar Al Karma"),
        new(Guid.Parse("1E7709BD-A05F-4C92-ACCA-B811287F43F9"), "تحقيقات نوح الألفي: قضية لوز مُر", "تحقيقات نوح الألفي: قضية لوز مُر", "The second Noah Al-Alfi mystery novel.", "ar", 2022, "Dar Al Karma"),
        new(Guid.Parse("2A992BB8-6A2F-4E24-A70C-CFB0860F9A34"), "تحقيقات نوح الألفي: قضية عنب الثعلب", "تحقيقات نوح الألفي: قضية عنب الثعلب", "The third Noah Al-Alfi mystery novel.", "ar", 2024, "Dar Al Karma"),
        new(Guid.Parse("E82F8B7A-C7CB-4E17-83EA-C0657607CFE3"), "تحقيقات نوح الألفي: قضية ذيل القط", "تحقيقات نوح الألفي: قضية ذيل القط", "The fourth Noah Al-Alfi mystery novel.", "ar", 2025, "Dar Al Karma"),
        new(Guid.Parse("DB5E91F3-B9EA-4F20-9ABF-2A6D3A29309A"), "تحقيقات نوح الألفي: قضية مخالب القط", "تحقيقات نوح الألفي: قضية مخالب القط", "The fifth Noah Al-Alfi mystery novel.", "ar", 2026, "Dar Al Karma"),
        new(Guid.Parse("860D83F8-47CF-4735-8DC7-C4FE8D634692"), "تحقيقات نوح الألفي: قضية السنوات السبع", "تحقيقات نوح الألفي: قضية السنوات السبع", "A Noah Al-Alfi mystery novel.", "ar", 2026, "Dar Al Karma"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000001"), "رحلتي من الشك إلى الإيمان", "رحلتي من الشك إلى الإيمان", "Mostafa Mahmoud's intellectual and spiritual autobiography.", "ar", 1970, "Dar Al Maaref"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000002"), "حوار مع صديقي الملحد", "حوار مع صديقي الملحد", "A philosophical dialogue about faith and skepticism.", "ar", 1974, "Dar Al Maaref"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000003"), "بين القصرين", "بين القصرين", "The first novel in Naguib Mahfouz's Cairo Trilogy.", "ar", 1956, "Dar El Shorouk"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000004"), "أولاد حارتنا", "أولاد حارتنا", "A landmark allegorical novel by Naguib Mahfouz.", "ar", 1959, "Dar El Shorouk"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000005"), "الأيام", "الأيام", "Taha Hussein's autobiographical work.", "ar", 1929, "Dar El Shorouk"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000006"), "دعاء الكروان", "دعاء الكروان", "A classic Arabic novel about injustice and revenge.", "ar", 1934, "Dar El Shorouk"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000007"), "Murder on the Orient Express", "Murder on the Orient Express", "Hercule Poirot investigates a murder aboard a snowbound train.", "en", 1934, "HarperCollins"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000008"), "And Then There Were None", "And Then There Were None", "Ten strangers are drawn to an isolated island by a mysterious host.", "en", 1939, "HarperCollins"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000009"), "Nineteen Eighty-Four", "Nineteen Eighty-Four", "A dystopian novel about surveillance and totalitarian power.", "en", 1949, "HarperCollins"),
        new(Guid.Parse("C0000000-0000-4000-8000-000000000010"), "Animal Farm", "Animal Farm", "A political satire told through a rebellion on an English farm.", "en", 1945, "HarperCollins")
    ];

    private static readonly BookAuthorSeed[] SeedBookAuthors =
    [
        new("زغازيغ", "Dr. Ahmad Khalid Tawfik", 1),
        new("يوتوبيا", "Dr. Ahmad Khalid Tawfik", 1),
        new("تحقيقات نوح الألفي: قضية ست الحسن", "Mirna Elmahdy", 1),
        new("تحقيقات نوح الألفي: قضية لوز مُر", "Mirna Elmahdy", 1),
        new("تحقيقات نوح الألفي: قضية عنب الثعلب", "Mirna Elmahdy", 1),
        new("تحقيقات نوح الألفي: قضية ذيل القط", "Mirna Elmahdy", 1),
        new("تحقيقات نوح الألفي: قضية مخالب القط", "Mirna Elmahdy", 1),
        new("تحقيقات نوح الألفي: قضية السنوات السبع", "Mirna Elmahdy", 1),
        new("رحلتي من الشك إلى الإيمان", "Dr. Mostafa Mahmoud", 1),
        new("حوار مع صديقي الملحد", "Dr. Mostafa Mahmoud", 1),
        new("بين القصرين", "Naguib Mahfouz", 1),
        new("أولاد حارتنا", "Naguib Mahfouz", 1),
        new("الأيام", "Taha Hussein", 1),
        new("دعاء الكروان", "Taha Hussein", 1),
        new("Murder on the Orient Express", "Agatha Christie", 1),
        new("And Then There Were None", "Agatha Christie", 1),
        new("Nineteen Eighty-Four", "George Orwell", 1),
        new("Animal Farm", "George Orwell", 1)
    ];

    private sealed record AuthorSeed(Guid Id, string Name, string Biography, DateOnly? BirthDate, DateOnly? DeathDate);
    private sealed record PublisherSeed(Guid Id, string Name, string? WebsiteUrl);
    private sealed record BookSeed(Guid Id, string Title, string OriginalTitle, string Description, string LanguageCode, int? FirstPublishedYear, string PublisherName);
    private sealed record BookAuthorSeed(string BookTitle, string AuthorName, int AuthorOrder);
}
