namespace LinkShortener.Application.Domain.Links;

public sealed record LinkExpirationResult(DateTime? ExpiresAtUtc, string? ErrorMessage)
{
    public bool IsValid => ErrorMessage is null;

    public static LinkExpirationResult Valid(DateTime? expiresAtUtc) => new(expiresAtUtc, null);

    public static LinkExpirationResult Invalid(string errorMessage) => new(null, errorMessage);
}
