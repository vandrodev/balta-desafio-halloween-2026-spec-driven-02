using System.Net;

namespace LinkShortener.Infrastructure.Links;

public sealed class SystemHostAddressResolver : IHostAddressResolver
{
    public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
    {
        return IPAddress.TryParse(host, out var address)
            ? Task.FromResult(new[] { address })
            : Dns.GetHostAddressesAsync(host, cancellationToken);
    }
}
