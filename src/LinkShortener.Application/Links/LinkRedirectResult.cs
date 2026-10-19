namespace LinkShortener.Application.Links;

public enum LinkRedirectFailure
{
    NotFound,
    Expired
}

public sealed record LinkRedirectResult(string? TargetUrl, LinkRedirectFailure? Failure)
{
    public bool Succeeded => TargetUrl is not null;

    public static LinkRedirectResult Success(string targetUrl) => new(targetUrl, null);

    public static LinkRedirectResult NotFound() => new(null, LinkRedirectFailure.NotFound);

    public static LinkRedirectResult Expired() => new(null, LinkRedirectFailure.Expired);
}
