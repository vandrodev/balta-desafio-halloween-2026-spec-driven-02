using LinkShortener.Application.Domain.Links;

namespace LinkShortener.Application.Links.Contracts;

public interface ILinkRepository
{
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken);

    Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken);

    Task<bool> TryIncrementAccessCountAsync(
        string code,
        DateTime accessedAtUtc,
        CancellationToken cancellationToken);

    Task<Link?> FindByCodeIfNotExpiredAsync(
        string code,
        DateTime accessedAtUtc,
        CancellationToken cancellationToken);

    Task<long?> GetAccessCountByCodeAsync(string code, CancellationToken cancellationToken);

    Task AddAsync(Link link, CancellationToken cancellationToken);
}
