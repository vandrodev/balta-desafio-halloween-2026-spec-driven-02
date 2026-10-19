namespace LinkShortener.Application.Links;

public sealed class LinkCodeValidator
{
    private const string AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
    private const int MinimumLength = 4;
    private const int MaximumLength = 20;

    public LinkCodeValidationResult Validate(string code)
    {
        ArgumentNullException.ThrowIfNull(code);

        if (code.Length is < MinimumLength or > MaximumLength)
        {
            return LinkCodeValidationResult.Invalid("O código personalizado deve ter entre 4 e 20 caracteres.");
        }

        if (code.Any(character => !AllowedCharacters.Contains(character)))
        {
            return LinkCodeValidationResult.Invalid(
                "O código personalizado deve conter somente letras maiúsculas de A a Z e números de 1 a 9.");
        }

        return LinkCodeValidationResult.Valid();
    }
}
