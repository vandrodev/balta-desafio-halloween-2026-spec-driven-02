using LinkShortener.Application.Links;
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
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
