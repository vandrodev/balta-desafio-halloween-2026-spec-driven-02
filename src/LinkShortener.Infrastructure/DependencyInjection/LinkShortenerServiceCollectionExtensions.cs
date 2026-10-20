using LinkShortener.Application.Domain.Links;
using LinkShortener.Application.Links.Contracts;
using LinkShortener.Application.Links.Services;
using LinkShortener.Infrastructure.Links;
using LinkShortener.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LinkShortener.Infrastructure.DependencyInjection;

public static class LinkShortenerServiceCollectionExtensions
{
    public static IServiceCollection AddLinkShortenerInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("LinkShortener")
            ?? throw new InvalidOperationException(
                "A connection string named 'LinkShortener' is required.");

        services.AddDbContext<LinkShortenerDbContext>(options =>
            options.UseSqlite(connectionString));
        services.AddSingleton<LinkRedirectCache>();
        services.AddSingleton<IHostAddressResolver, SystemHostAddressResolver>();
        services.AddSingleton<IUrlAccessibilityChecker, PublicUrlAccessibilityChecker>();
        services.AddSingleton<LinkCodeGenerator>();
        services.AddTransient<TargetUrlValidator>();
        services.AddSingleton<LinkCodeValidator>();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<LinkExpirationPolicy>();
        services.AddScoped<ILinkRepository, LinkRepository>();
        services.AddScoped<LinkService>();

        return services;
    }

    public static void InitializeLinkShortenerDatabase(this IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
        dbContext.Database.Migrate();
    }
}
