namespace LinkShortener.Application.Links;

public interface IUrlAccessibilityChecker
{
    Task<bool> IsPubliclyAccessibleAsync(Uri targetUri, CancellationToken cancellationToken);
}
