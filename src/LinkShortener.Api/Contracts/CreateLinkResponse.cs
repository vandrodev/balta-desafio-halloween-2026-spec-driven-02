namespace LinkShortener.Api.Contracts;

public sealed record CreateLinkResponse(string ShortUrl, string Code);
