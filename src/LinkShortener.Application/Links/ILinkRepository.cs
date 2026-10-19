namespace LinkShortener.Application.Links;

public interface ILinkRepository
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(Link link, CancellationToken cancellationToken);
}
