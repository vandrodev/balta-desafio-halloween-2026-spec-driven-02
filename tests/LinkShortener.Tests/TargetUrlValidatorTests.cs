using System.Net;
using System.Net.Http;
using LinkShortener.Application.Links;
using LinkShortener.Infrastructure.Links;

namespace LinkShortener.Tests;

public sealed class TargetUrlValidatorTests
{
    [Theory]
    [InlineData("http://example.com/path")]
    [InlineData("https://example.com/path")]
    public async Task ValidateAsync_AcceptsAccessibleHttpAndHttpsUrls(string targetUrl)
    {
        var checker = new StubUrlAccessibilityChecker(isAccessible: true);
        var validator = new TargetUrlValidator(checker);

        var result = await validator.ValidateAsync(targetUrl);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
        Assert.Equal(targetUrl, checker.CheckedUri?.AbsoluteUri);
    }

    [Fact]
    public async Task ValidateAsync_AcceptsUrlAtMaximumLength()
    {
        var checker = new StubUrlAccessibilityChecker(isAccessible: true);
        var validator = new TargetUrlValidator(checker);
        var prefix = "https://example.com/";
        var targetUrl = prefix + new string('a', 2048 - prefix.Length);

        var result = await validator.ValidateAsync(targetUrl);

        Assert.True(result.IsValid);
        Assert.Equal(2048, targetUrl.Length);
    }

    [Theory]
    [InlineData("ftp://example.com/file")]
    [InlineData("file:///tmp/file")]
    public async Task ValidateAsync_RejectsUnsupportedSchemes(string targetUrl)
    {
        var checker = new StubUrlAccessibilityChecker(isAccessible: true);
        var validator = new TargetUrlValidator(checker);

        var result = await validator.ValidateAsync(targetUrl);

        Assert.False(result.IsValid);
        Assert.Equal("A URL de destino deve usar http ou https.", result.ErrorMessage);
        Assert.Null(checker.CheckedUri);
    }

    [Fact]
    public async Task ValidateAsync_RejectsUrlsLongerThan2048Characters()
    {
        var checker = new StubUrlAccessibilityChecker(isAccessible: true);
        var validator = new TargetUrlValidator(checker);

        var result = await validator.ValidateAsync($"https://example.com/{new string('a', 2030)}");

        Assert.False(result.IsValid);
        Assert.Equal("A URL de destino não pode exceder 2048 caracteres.", result.ErrorMessage);
        Assert.Null(checker.CheckedUri);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("https://user:password@example.com/")]
    public async Task ValidateAsync_RejectsMissingMalformedOrCredentialedUrls(string? targetUrl)
    {
        var checker = new StubUrlAccessibilityChecker(isAccessible: true);
        var validator = new TargetUrlValidator(checker);

        var result = await validator.ValidateAsync(targetUrl);

        Assert.False(result.IsValid);
        Assert.NotNull(result.ErrorMessage);
        Assert.Null(checker.CheckedUri);
    }

    [Fact]
    public async Task ValidateAsync_RejectsUrlWhenAccessibilityCheckFails()
    {
        var validator = new TargetUrlValidator(new StubUrlAccessibilityChecker(isAccessible: false));

        var result = await validator.ValidateAsync("https://example.com/");

        Assert.False(result.IsValid);
        Assert.Equal("A URL de destino não está publicamente acessível.", result.ErrorMessage);
    }

    [Fact]
    public async Task AccessibilityChecker_SendsHeadForPublicAddress()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK);
        using var checker = new PublicUrlAccessibilityChecker(
            new StubHostAddressResolver(IPAddress.Parse("8.8.8.8")),
            handler);

        var isAccessible = await checker.IsPubliclyAccessibleAsync(
            new Uri("https://example.com/"),
            CancellationToken.None);

        Assert.True(isAccessible);
        Assert.Equal(HttpMethod.Head, handler.Method);
        Assert.Equal("https://example.com/", handler.RequestUri?.AbsoluteUri);
    }

    [Fact]
    public async Task AccessibilityChecker_RejectsPrivateResolvedAddressWithoutRequestingIt()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK);
        using var checker = new PublicUrlAccessibilityChecker(
            new StubHostAddressResolver(IPAddress.Parse("127.0.0.1")),
            handler);

        var isAccessible = await checker.IsPubliclyAccessibleAsync(
            new Uri("https://example.com/"),
            CancellationToken.None);

        Assert.False(isAccessible);
        Assert.False(handler.WasCalled);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task AccessibilityChecker_RejectsUnsuccessfulHeadResponses(HttpStatusCode statusCode)
    {
        using var checker = new PublicUrlAccessibilityChecker(
            new StubHostAddressResolver(IPAddress.Parse("8.8.8.8")),
            new StubHttpMessageHandler(statusCode));

        var isAccessible = await checker.IsPubliclyAccessibleAsync(
            new Uri("https://example.com/"),
            CancellationToken.None);

        Assert.False(isAccessible);
    }

    private sealed class StubUrlAccessibilityChecker(bool isAccessible) : IUrlAccessibilityChecker
    {
        public Uri? CheckedUri { get; private set; }

        public Task<bool> IsPubliclyAccessibleAsync(Uri targetUri, CancellationToken cancellationToken)
        {
            CheckedUri = targetUri;
            return Task.FromResult(isAccessible);
        }
    }

    private sealed class StubHostAddressResolver(params IPAddress[] addresses) : IHostAddressResolver
    {
        public Task<IPAddress[]> ResolveAsync(string host, CancellationToken cancellationToken)
        {
            return Task.FromResult(addresses);
        }
    }

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode) : HttpMessageHandler
    {
        public bool WasCalled { get; private set; }

        public HttpMethod? Method { get; private set; }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            Method = request.Method;
            RequestUri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(statusCode));
        }
    }
}
