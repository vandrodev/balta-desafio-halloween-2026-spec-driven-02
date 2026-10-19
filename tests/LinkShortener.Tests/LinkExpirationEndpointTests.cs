using System.Net;
using System.Net.Http.Json;
using LinkShortener.Application.Links;
using LinkShortener.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace LinkShortener.Tests;

public sealed class LinkExpirationEndpointTests
{
    [Fact]
    public async Task PostLink_RejectsPastUtcDateAndExpiresAtEndOfRequestedUtcDay()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"linkshortener-{Guid.NewGuid():N}.db");
        var clock = new TestTimeProvider(new DateTimeOffset(2026, 10, 18, 23, 59, 59, TimeSpan.Zero)
            .AddTicks(8_000_000));
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<LinkShortenerDbContext>>();
                services.AddDbContext<LinkShortenerDbContext>(options =>
                    options.UseSqlite($"Data Source={databasePath};Pooling=False"));

                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(clock);

                services.RemoveAll<IUrlAccessibilityChecker>();
                services.AddSingleton<IUrlAccessibilityChecker, AccessibleUrlChecker>();
            });
        });

        try
        {
            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });

            using var pastDateResponse = await client.PostAsJsonAsync(
                "/api/links",
                new
                {
                    targetUrl = "https://example.com/past",
                    code = "PAST01",
                    expiresAtUtc = "2026-10-17T00:00:00Z"
                });
            Assert.Equal(HttpStatusCode.BadRequest, pastDateResponse.StatusCode);
            Assert.Equal(
                "application/problem+json",
                pastDateResponse.Content.Headers.ContentType?.MediaType);

            using var createResponse = await client.PostAsJsonAsync(
                "/api/links",
                new
                {
                    targetUrl = "https://example.com/valid-through-utc-day",
                    code = "VALID1",
                    expiresAtUtc = "2026-10-18T00:01:00Z"
                });
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            using (var scope = factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
                var link = await dbContext.Links.SingleAsync(candidate => candidate.Code == "VALID1");
                Assert.Equal(
                    new DateTime(2026, 10, 18, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999),
                    link.ExpiresAtUtc);
                Assert.Empty(await dbContext.Links.Where(candidate => candidate.Code == "PAST01").ToListAsync());
            }

            using var beforeExpiryResponse = await client.GetAsync("/VALID1");
            Assert.Equal(HttpStatusCode.Found, beforeExpiryResponse.StatusCode);

            clock.SetUtcNow(new DateTimeOffset(2026, 10, 18, 23, 59, 59, TimeSpan.Zero)
                .AddTicks(9_999_999));
            using var atExpiryResponse = await client.GetAsync("/VALID1");
            Assert.Equal(HttpStatusCode.Found, atExpiryResponse.StatusCode);

            clock.SetUtcNow(new DateTimeOffset(2026, 10, 19, 0, 0, 0, TimeSpan.Zero));
            using var afterExpiryResponse = await client.GetAsync("/VALID1");
            Assert.Equal(HttpStatusCode.Gone, afterExpiryResponse.StatusCode);

            using var verificationScope = factory.Services.CreateScope();
            var verificationDbContext = verificationScope.ServiceProvider
                .GetRequiredService<LinkShortenerDbContext>();
            var accessCount = await verificationDbContext.Links
                .Where(link => link.Code == "VALID1")
                .Select(link => link.AccessCount)
                .SingleAsync();
            Assert.Equal(2, accessCount);
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

    private sealed class TestTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void SetUtcNow(DateTimeOffset utcNow) => _utcNow = utcNow;
    }

    private sealed class AccessibleUrlChecker : IUrlAccessibilityChecker
    {
        public Task<bool> IsPubliclyAccessibleAsync(Uri targetUri, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
