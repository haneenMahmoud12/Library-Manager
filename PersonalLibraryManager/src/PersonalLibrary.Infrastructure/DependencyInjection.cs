using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PersonalLibrary.Application.Abstractions;
using PersonalLibrary.Application.Catalog.Repositories;
using PersonalLibrary.Application.Catalog.Metadata;
using PersonalLibrary.Application.Identity.Services;
using PersonalLibrary.Application.Libraries.Repositories;
using PersonalLibrary.Application.Persistence.IRepositories;
using PersonalLibrary.Infrastructure.Authentication;
using PersonalLibrary.Infrastructure.Books.Metadata;
using PersonalLibrary.Infrastructure.Email;
using PersonalLibrary.Infrastructure.Identity;
using PersonalLibrary.Infrastructure.Identity.Services;
using PersonalLibrary.Infrastructure.Persistence;
using PersonalLibrary.Infrastructure.Persistence.Repositories;
using PersonalLibrary.Infrastructure.Persistence.Repositories.Catalog;

namespace PersonalLibrary.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Connection string 'DefaultConnection' was not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<JwtOptions>(
            configuration.GetSection(JwtOptions.SectionName));
        services.Configure<SmtpEmailOptions>(
            configuration.GetSection(SmtpEmailOptions.SectionName));
        services.Configure<BookMetadataOptions>(
            configuration.GetSection(BookMetadataOptions.SectionName));

        var metadataTimeoutSeconds = Math.Clamp(
            configuration.GetValue<int?>("BookMetadata:RequestTimeoutSeconds") ?? 10,
            1,
            60);

        services.AddHttpClient<GoogleBooksMetadataProvider>(client =>
        {
            client.BaseAddress = new Uri("https://www.googleapis.com/books/v1/");
            client.Timeout = TimeSpan.FromSeconds(metadataTimeoutSeconds);
        });
        services.AddHttpClient<OpenLibraryMetadataProvider>(client =>
        {
            client.BaseAddress = new Uri("https://openlibrary.org/");
            client.Timeout = TimeSpan.FromSeconds(metadataTimeoutSeconds);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("PersonalLibraryManager/1.0");
        });
        services.AddTransient<IBookMetadataSource>(provider =>
            provider.GetRequiredService<GoogleBooksMetadataProvider>());
        services.AddTransient<IBookMetadataSource>(provider =>
            provider.GetRequiredService<OpenLibraryMetadataProvider>());
        services.AddScoped<IBookMetadataProvider, FallbackBookMetadataProvider>();

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<ILibraryRepository, LibraryRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IPublisherRepository, PublisherRepository>();
        services.AddScoped<IBookEditionRepository, BookEditionRepository>();
        services.AddScoped<IBookCopyRepository, BookCopyRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IEmailService, SmtpEmailService>();

        services.AddDataProtection();

        services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;

                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = false;

                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddRoles<ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddSignInManager()
            .AddDefaultTokenProviders();

        return services;
    }
}
