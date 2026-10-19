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
