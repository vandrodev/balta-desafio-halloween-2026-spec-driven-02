using System.Net;
using System.Net.Http.Json;
using LinkShortener.Application.Links;
using LinkShortener.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
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
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:LinkShortener"] = $"Data Source={databasePath};Pooling=False"
                });
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IUrlAccessibilityChecker>();
                services.AddSingleton<IUrlAccessibilityChecker, AccessibleUrlChecker>();
            });
        });

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

    private sealed record CreateLinkResponse(string ShortUrl, string Code);

    private sealed class AccessibleUrlChecker : IUrlAccessibilityChecker
    {
        public Task<bool> IsPubliclyAccessibleAsync(Uri targetUri, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
