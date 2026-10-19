using System.Net;

namespace LinkShortener.Infrastructure.Links;

public interface IHostAddressResolver
{
    Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken);
}
