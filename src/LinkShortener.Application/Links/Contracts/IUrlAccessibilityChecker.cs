namespace LinkShortener.Application.Links.Contracts;

public interface IUrlAccessibilityChecker
{
    Task<bool> IsPubliclyAccessibleAsync(Uri targetUri, CancellationToken cancellationToken);
}
