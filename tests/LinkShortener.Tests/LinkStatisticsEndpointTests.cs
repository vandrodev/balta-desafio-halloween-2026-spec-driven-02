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

public sealed class LinkStatisticsEndpointTests
{
    [Fact]
    public async Task GetStatistics_ReturnsPublicAccessCountWithoutAuthentication()
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
            using (var scope = factory.Services.CreateScope())
            {
                var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
                dbContext.Links.Add(new Link
                {
                    Code = "STATS1",
                    TargetUrl = "https://example.com/destination",
                    CreatedAtUtc = DateTime.UtcNow,
                    AccessCount = 17
                });
                await dbContext.SaveChangesAsync();
            }

            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
            using var redirectResponse = await client.GetAsync("/STATS1");
            Assert.Equal(HttpStatusCode.Found, redirectResponse.StatusCode);

            using var response = await client.GetAsync("/api/links/STATS1/stats");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            var statistics = await response.Content.ReadFromJsonAsync<LinkStatisticsResponse>();
            Assert.NotNull(statistics);
            Assert.Equal(18, statistics.AccessCount);
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

    [Fact]
    public async Task GetStatistics_ReturnsNotFoundForUnknownCode()
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
            using var client = factory.CreateClient();
            using var response = await client.GetAsync("/api/links/UNKNOWN1/stats");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
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

    private sealed record LinkStatisticsResponse(long AccessCount);
}
