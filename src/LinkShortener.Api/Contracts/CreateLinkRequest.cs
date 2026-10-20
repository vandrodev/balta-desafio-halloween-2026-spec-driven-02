namespace LinkShortener.Api.Contracts;

public sealed record CreateLinkRequest(
    string? TargetUrl,
    string? Code = null,
    DateTimeOffset? ExpiresAtUtc = null);
