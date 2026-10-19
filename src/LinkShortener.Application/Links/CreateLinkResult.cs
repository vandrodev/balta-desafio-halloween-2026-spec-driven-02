namespace LinkShortener.Application.Links;

public enum CreateLinkFailure
{
    InvalidTargetUrl,
    InvalidCode,
    CodeAlreadyExists
}

public sealed record CreateLinkResult(Link? Link, string? ErrorMessage, CreateLinkFailure? Failure)
{
    public bool Succeeded => Link is not null;

    public static CreateLinkResult Success(Link link) => new(link, null, null);

    public static CreateLinkResult InvalidTargetUrl(string errorMessage) =>
        new(null, errorMessage, CreateLinkFailure.InvalidTargetUrl);

    public static CreateLinkResult InvalidCode(string errorMessage) =>
        new(null, errorMessage, CreateLinkFailure.InvalidCode);

    public static CreateLinkResult CodeAlreadyExists() =>
        new(null, "O código informado já está em uso.", CreateLinkFailure.CodeAlreadyExists);
}
