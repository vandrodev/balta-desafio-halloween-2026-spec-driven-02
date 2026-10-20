using LinkShortener.Application.Domain.Links;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Infrastructure.Persistence;

public sealed class LinkShortenerDbContext(DbContextOptions<LinkShortenerDbContext> options)
    : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LinkShortenerDbContext).Assembly);
    }
}
