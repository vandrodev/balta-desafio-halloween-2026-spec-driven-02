using System.Net;
using LinkShortener.Application.Links;
using LinkShortener.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinkShortener.Tests;

public sealed class LinkRedirectEndpointTests
{
    [Fact]
    public async Task GetCode_RedirectsExistingLinkAndReturnsNotFoundOrGoneAsAppropriate()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"linkshortener-{Guid.NewGuid():N}.db");
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<LinkShortenerDbContext>>();
                services.AddDbContext<LinkShortenerDbContext>(options =>
                    options.UseSqlite($"Data Source={databasePath};Pooling=False"));
            });
        });

        try
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using (var scope = factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
                dbContext.Links.AddRange(
                    new Link
                    {
                        Code = "VALID1",
                        TargetUrl = "https://example.com/destination",
                        CreatedAtUtc = DateTime.UtcNow,
                        ExpiresAtUtc = DateTime.UtcNow.AddMinutes(5)
                    },
                    new Link
                    {
                        Code = "EXPRD1",
                        TargetUrl = "https://example.com/expired",
                        CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
                        ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-5)
                    });
                await dbContext.SaveChangesAsync();
            }

            using var redirectResponse = await client.GetAsync("/VALID1");
            Assert.Equal(HttpStatusCode.Found, redirectResponse.StatusCode);
            Assert.Equal(
                "https://example.com/destination",
                redirectResponse.Headers.Location?.AbsoluteUri);

            using var notFoundResponse = await client.GetAsync("/MISSING1");
            Assert.Equal(HttpStatusCode.NotFound, notFoundResponse.StatusCode);
            Assert.Equal("application/problem+json", notFoundResponse.Content.Headers.ContentType?.MediaType);

            using var expiredResponse = await client.GetAsync("/EXPRD1");
            Assert.Equal(HttpStatusCode.Gone, expiredResponse.StatusCode);
            Assert.Equal("application/problem+json", expiredResponse.Content.Headers.ContentType?.MediaType);
        }
        finally
        {
            await factory.DisposeAsync();
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }
}
