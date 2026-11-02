using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PersonalLibrary.Application.Identity.Models;
using PersonalLibrary.Application.Identity.Services;
using PersonalLibrary.Infrastructure.Persistence;

namespace PersonalLibrary.IntegrationTests.Identity;

public sealed class RegistrationApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"registration-acceptance-{Guid.NewGuid():N}";
    private readonly InMemoryDatabaseRoot _databaseRoot = new();

    public RecordingEmailService Email { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName, _databaseRoot));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Email);
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Database.EnsureCreated();
        return host;
    }
}

public sealed class RecordingEmailService : IEmailService
{
    private readonly ConcurrentQueue<EmailConfirmationChallenge> _messages = new();

    public IReadOnlyCollection<EmailConfirmationChallenge> Messages => _messages.ToArray();

    public Task SendConfirmationEmailAsync(
        EmailConfirmationChallenge challenge,
        CancellationToken cancellationToken = default)
    {
        _messages.Enqueue(challenge);
        return Task.CompletedTask;
    }
}
