namespace LinkShortener.Application.Links;

public sealed class TargetUrlValidator(IUrlAccessibilityChecker accessibilityChecker)
{
    private const int MaximumLength = 2048;

    public async Task<TargetUrlValidationResult> ValidateAsync(
        string? targetUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(targetUrl))
        {
            return TargetUrlValidationResult.Invalid("A URL de destino é obrigatória.");
        }

        if (targetUrl.Length > MaximumLength)
        {
            return TargetUrlValidationResult.Invalid("A URL de destino não pode exceder 2048 caracteres.");
        }

        if (!Uri.TryCreate(targetUrl, UriKind.Absolute, out var targetUri)
            || !string.IsNullOrEmpty(targetUri.UserInfo))
        {
            return TargetUrlValidationResult.Invalid("A URL de destino é inválida.");
        }

        if (targetUri.Scheme is not ("http" or "https"))
        {
            return TargetUrlValidationResult.Invalid("A URL de destino deve usar http ou https.");
        }

        if (!await accessibilityChecker.IsPubliclyAccessibleAsync(targetUri, cancellationToken))
        {
            return TargetUrlValidationResult.Invalid("A URL de destino não está publicamente acessível.");
        }

        return TargetUrlValidationResult.Valid();
    }
}
