using System.Net;
using System.Net.Http.Json;
using LinkShortener.Application.Links;
using LinkShortener.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Tests;

public sealed class CreateLinkEndpointTests
{
    [Fact]
    public async Task PostLinks_WithAccessibleTarget_ReturnsCreatedLink()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"linkshortener-{Guid.NewGuid():N}.db");
        var factory = CreateFactory(databasePath);

        try
        {
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync(
                "/api/links",
                new { targetUrl = "https://example.com/resource" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var createdLink = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
            Assert.NotNull(createdLink);
            Assert.Matches("^[A-Z1-9]{7}$", createdLink.Code);
            Assert.Equal($"{client.BaseAddress}{createdLink.Code}", createdLink.ShortUrl);
            Assert.Equal(createdLink.ShortUrl, response.Headers.Location?.ToString());

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
            var persistedLink = await dbContext.Links.SingleAsync(link => link.Code == createdLink.Code);
            Assert.Equal("https://example.com/resource", persistedLink.TargetUrl);
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
    public async Task PostLinks_WithCustomCode_ReturnsCreatedLinkAndPersistsCode()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"linkshortener-{Guid.NewGuid():N}.db");
        var factory = CreateFactory(databasePath);

        try
        {
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync(
                "/api/links",
                new { targetUrl = "https://example.com/resource", code = "HELLO7" });

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var createdLink = await response.Content.ReadFromJsonAsync<CreateLinkResponse>();
            Assert.NotNull(createdLink);
            Assert.Equal("HELLO7", createdLink.Code);
            Assert.Equal($"{client.BaseAddress}HELLO7", createdLink.ShortUrl);

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
            var persistedLink = await dbContext.Links.SingleAsync(link => link.Code == "HELLO7");
            Assert.Equal("https://example.com/resource", persistedLink.TargetUrl);
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
    public async Task PostLinks_WithDuplicateCustomCode_ReturnsConflict()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"linkshortener-{Guid.NewGuid():N}.db");
        var factory = CreateFactory(databasePath);

        try
        {
            using var client = factory.CreateClient();
            var request = new { targetUrl = "https://example.com/resource", code = "HELLO7" };

            using var firstResponse = await client.PostAsJsonAsync("/api/links", request);
            using var duplicateResponse = await client.PostAsJsonAsync("/api/links", request);

            Assert.Equal(HttpStatusCode.Created, firstResponse.StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, duplicateResponse.StatusCode);
            Assert.Equal("application/problem+json", duplicateResponse.Content.Headers.ContentType?.MediaType);

            using var scope = factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
            Assert.Equal(1, await dbContext.Links.CountAsync(link => link.Code == "HELLO7"));
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

    [Theory]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTUVWXYZ1")]
    [InlineData("ABC0")]
    [InlineData("abc1")]
    [InlineData("AB C")]
    [InlineData("AB-C")]
    public async Task PostLinks_WithInvalidCustomCode_ReturnsBadRequest(string code)
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"linkshortener-{Guid.NewGuid():N}.db");
        var factory = CreateFactory(databasePath);

        try
        {
            using var client = factory.CreateClient();
            using var response = await client.PostAsJsonAsync(
                "/api/links",
                new { targetUrl = "https://example.com/resource", code });

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
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

    private sealed record CreateLinkResponse(string ShortUrl, string Code);

    private static WebApplicationFactory<Program> CreateFactory(string databasePath)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<DbContextOptions<LinkShortenerDbContext>>();
                services.AddDbContext<LinkShortenerDbContext>(options =>
                    options.UseSqlite($"Data Source={databasePath};Pooling=False"));

                services.RemoveAll<IUrlAccessibilityChecker>();
                services.AddSingleton<IUrlAccessibilityChecker, AccessibleUrlChecker>();
            });
        });
    }

    private sealed class AccessibleUrlChecker : IUrlAccessibilityChecker
    {
        public Task<bool> IsPubliclyAccessibleAsync(Uri targetUri, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
