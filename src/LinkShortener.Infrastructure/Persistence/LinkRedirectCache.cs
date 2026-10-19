using Microsoft.Extensions.Caching.Memory;

namespace LinkShortener.Infrastructure.Persistence;

public sealed class LinkRedirectCache : IDisposable
{
    private const int MaximumEntries = 1024;
    private static readonly TimeSpan SlidingExpiration = TimeSpan.FromMinutes(10);
    private const string CacheKeyPrefix = "LinkRedirect:";

    private readonly MemoryCache _cache = new(new MemoryCacheOptions
    {
        SizeLimit = MaximumEntries
    });

    public CachedRedirect? FindValid(string code, DateTime accessedAtUtc)
    {
        if (!_cache.TryGetValue(GetCacheKey(code), out CachedRedirect? cachedRedirect)
            || cachedRedirect is null)
        {
            return null;
        }

        return cachedRedirect.ExpiresAtUtc is null || cachedRedirect.ExpiresAtUtc >= accessedAtUtc
            ? cachedRedirect
            : null;
    }

    public void Set(string code, string targetUrl, DateTime? expiresAtUtc)
    {
        _cache.Set(
            GetCacheKey(code),
            new CachedRedirect(targetUrl, expiresAtUtc),
            new MemoryCacheEntryOptions()
                .SetSize(1)
                .SetSlidingExpiration(SlidingExpiration));
    }

    public void Remove(string code)
    {
        _cache.Remove(GetCacheKey(code));
    }

    public void Dispose()
    {
        _cache.Dispose();
    }

    private static string GetCacheKey(string code) => $"{CacheKeyPrefix}{code}";

    public sealed record CachedRedirect(string TargetUrl, DateTime? ExpiresAtUtc);
}
