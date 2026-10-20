using LinkShortener.Infrastructure.Persistence;

namespace LinkShortener.Tests.Unit.Infrastructure;

public sealed class LinkRedirectCacheTests
{
    [Fact]
    public void FindValid_ReturnsCachedDestinationForActiveLink()
    {
        using var cache = new LinkRedirectCache();
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(5);
        cache.Set("CODE1", "https://example.com/destination", expiresAtUtc);

        var cachedRedirect = cache.FindValid("CODE1", DateTime.UtcNow);

        Assert.NotNull(cachedRedirect);
        Assert.Equal("https://example.com/destination", cachedRedirect.TargetUrl);
        Assert.Equal(expiresAtUtc, cachedRedirect.ExpiresAtUtc);
    }

    [Fact]
    public void FindValid_DoesNotReturnExpiredCachedDestination()
    {
        using var cache = new LinkRedirectCache();
        cache.Set(
            "CODE1",
            "https://example.com/destination",
            DateTime.UtcNow.AddMinutes(-1));

        var cachedRedirect = cache.FindValid("CODE1", DateTime.UtcNow);

        Assert.Null(cachedRedirect);
    }

    [Fact]
    public void Remove_InvalidatesCachedDestination()
    {
        using var cache = new LinkRedirectCache();
        cache.Set("CODE1", "https://example.com/destination", expiresAtUtc: null);

        cache.Remove("CODE1");

        Assert.Null(cache.FindValid("CODE1", DateTime.UtcNow));
    }
}
