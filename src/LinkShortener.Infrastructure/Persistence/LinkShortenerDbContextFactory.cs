using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LinkShortener.Infrastructure.Persistence;

public sealed class LinkShortenerDbContextFactory : IDesignTimeDbContextFactory<LinkShortenerDbContext>
{
    public LinkShortenerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<LinkShortenerDbContext>()
            .UseSqlite("Data Source=linkshortener.db")
            .Options;

        return new LinkShortenerDbContext(options);
    }
}
