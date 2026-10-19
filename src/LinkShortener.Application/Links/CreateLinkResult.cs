namespace LinkShortener.Application.Links;

public sealed record CreateLinkResult(Link? Link, string? ErrorMessage)
{
    public bool Succeeded => Link is not null;

    public static CreateLinkResult Success(Link link) => new(link, null);

    public static CreateLinkResult Failure(string errorMessage) => new(null, errorMessage);
}
