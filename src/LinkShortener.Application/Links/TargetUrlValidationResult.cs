namespace LinkShortener.Application.Links;

public sealed record TargetUrlValidationResult(bool IsValid, string? ErrorMessage)
{
    public static TargetUrlValidationResult Valid() => new(true, null);

    public static TargetUrlValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}
