using LinkShortener.Application.Links;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Infrastructure.Persistence;

public sealed class LinkRepository(LinkShortenerDbContext dbContext) : ILinkRepository
{
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
    {
        return dbContext.Links.AnyAsync(link => link.Code == code, cancellationToken);
    }

    public Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken)
    {
        return dbContext.Links.SingleOrDefaultAsync(link => link.Code == code, cancellationToken);
    }

    public async Task<bool> TryIncrementAccessCountAsync(
        string code,
        DateTime accessedAtUtc,
        CancellationToken cancellationToken)
    {
        var updatedRows = await dbContext.Links
            .Where(link => link.Code == code
                && (link.ExpiresAtUtc == null || link.ExpiresAtUtc > accessedAtUtc))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(link => link.AccessCount, link => link.AccessCount + 1),
                cancellationToken);

        return updatedRows == 1;
    }

    public Task<Link?> FindByCodeIfNotExpiredAsync(
        string code,
        DateTime accessedAtUtc,
        CancellationToken cancellationToken)
    {
        return dbContext.Links.SingleOrDefaultAsync(
            link => link.Code == code
                && (link.ExpiresAtUtc == null || link.ExpiresAtUtc > accessedAtUtc),
            cancellationToken);
    }

    public async Task<long?> GetAccessCountByCodeAsync(
        string code,
        CancellationToken cancellationToken)
    {
        return await dbContext.Links
            .Where(link => link.Code == code)
            .Select(link => (long?)link.AccessCount)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task AddAsync(Link link, CancellationToken cancellationToken)
    {
        dbContext.Links.Add(link);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsCodeUniqueConstraintViolation(exception))
        {
            dbContext.Entry(link).State = EntityState.Detached;
            throw new LinkCodeAlreadyExistsException();
        }
    }

    private static bool IsCodeUniqueConstraintViolation(DbUpdateException exception)
    {
        return exception.InnerException is SqliteException
        {
            SqliteErrorCode: 19,
            SqliteExtendedErrorCode: 2067
        };
    }
}
