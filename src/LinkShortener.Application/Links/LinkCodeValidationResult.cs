namespace LinkShortener.Application.Links;

public sealed record LinkCodeValidationResult(bool IsValid, string? ErrorMessage)
{
    public static LinkCodeValidationResult Valid() => new(true, null);

    public static LinkCodeValidationResult Invalid(string errorMessage) => new(false, errorMessage);
}
