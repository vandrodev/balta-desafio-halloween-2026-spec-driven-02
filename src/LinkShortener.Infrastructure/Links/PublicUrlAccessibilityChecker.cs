using System.Net;
using System.Net.Sockets;
using System.Net.Http;
using LinkShortener.Application.Links;

namespace LinkShortener.Infrastructure.Links;

public sealed class PublicUrlAccessibilityChecker : IUrlAccessibilityChecker, IDisposable
{
    private static readonly HttpRequestOptionsKey<IPAddress[]> ValidatedAddressesKey =
        new("LinkShortener.ValidatedAddresses");
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

    private readonly IHostAddressResolver _hostAddressResolver;
    private readonly HttpClient _httpClient;

    public PublicUrlAccessibilityChecker(IHostAddressResolver hostAddressResolver)
        : this(hostAddressResolver, CreateSocketsHandler())
    {
    }

    public PublicUrlAccessibilityChecker(
        IHostAddressResolver hostAddressResolver,
        HttpMessageHandler handler)
    {
        _hostAddressResolver = hostAddressResolver;
        _httpClient = new HttpClient(handler, disposeHandler: true)
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
    }

    public async Task<bool> IsPubliclyAccessibleAsync(
        Uri targetUri,
        CancellationToken cancellationToken)
    {
        IPAddress[] addresses;
        try
        {
            addresses = await _hostAddressResolver.ResolveAsync(targetUri.DnsSafeHost, cancellationToken);
        }
        catch (SocketException)
        {
            return false;
        }

        if (addresses.Length == 0 || addresses.Any(address => !IsPublicAddress(address)))
        {
            return false;
        }

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(RequestTimeout);

        using var request = new HttpRequestMessage(HttpMethod.Head, targetUri);
        request.Options.Set(ValidatedAddressesKey, addresses);

        try
        {
            using var response = await _httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                timeout.Token);

            return (int)response.StatusCode is >= 200 and < 400;
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private static SocketsHttpHandler CreateSocketsHandler()
    {
        return new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            UseProxy = false,
            ConnectCallback = ConnectToValidatedAddressAsync
        };
    }

    private static async ValueTask<Stream> ConnectToValidatedAddressAsync(
        SocketsHttpConnectionContext context,
        CancellationToken cancellationToken)
    {
        if (!context.InitialRequestMessage.Options.TryGetValue(ValidatedAddressesKey, out var addresses))
        {
            throw new HttpRequestException("O endereço do host não foi validado.");
        }

        Exception? lastException = null;
        foreach (var address in addresses)
        {
            var socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                await socket.ConnectAsync(
                    new IPEndPoint(address, context.DnsEndPoint.Port),
                    cancellationToken);
                return new NetworkStream(socket, ownsSocket: true);
            }
            catch (SocketException exception)
            {
                lastException = exception;
                socket.Dispose();
            }
            catch (OperationCanceledException)
            {
                socket.Dispose();
                throw;
            }
        }

        throw new HttpRequestException("Não foi possível conectar ao host público.", lastException);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (address.IsIPv4MappedToIPv6)
        {
            return IsPublicAddress(address.MapToIPv4());
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return !IPAddress.IsLoopback(address)
                && !address.IsIPv6LinkLocal
                && !address.IsIPv6SiteLocal
                && !address.IsIPv6Multicast
                && !address.IsIPv6Teredo
                && !address.IsIPv6UniqueLocal
                && IsGlobalUnicastIpv6(address);
        }

        if (address.AddressFamily != AddressFamily.InterNetwork)
        {
            return false;
        }

        var bytes = address.GetAddressBytes();
        return bytes[0] is not (0 or 10 or 127 or >= 224)
            && !(bytes[0] == 100 && bytes[1] is >= 64 and <= 127)
            && !(bytes[0] == 169 && bytes[1] == 254)
            && !(bytes[0] == 172 && bytes[1] is >= 16 and <= 31)
            && !(bytes[0] == 192 && bytes[1] == 168)
            && !(bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 0)
            && !(bytes[0] == 192 && bytes[1] == 0 && bytes[2] == 2)
            && !(bytes[0] == 192 && bytes[1] == 88 && bytes[2] == 99)
            && !(bytes[0] == 198 && bytes[1] is 18 or 19)
            && !(bytes[0] == 198 && bytes[1] == 51 && bytes[2] == 100)
            && !(bytes[0] == 203 && bytes[1] == 0 && bytes[2] == 113);
    }

    private static bool IsGlobalUnicastIpv6(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        var isGlobalUnicast = (bytes[0] & 0xE0) == 0x20;
        var isDocumentationRange = bytes[0] == 0x20
            && bytes[1] == 0x01
            && bytes[2] == 0x0D
            && bytes[3] == 0xB8;

        return isGlobalUnicast && !isDocumentationRange;
    }
}
