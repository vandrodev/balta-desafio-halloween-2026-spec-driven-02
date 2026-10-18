using LinkShortener.Application.Links;
using LinkShortener.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LinkShortener.Tests;

public sealed class LinkPersistenceTests
{
    [Fact]
    public async Task Migrate_CreatesLinksTableAndUniqueCodeIndex()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<LinkShortenerDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var dbContext = new LinkShortenerDbContext(options);
        await dbContext.Database.MigrateAsync();

        var tableName = await GetScalarAsync(
            connection,
            "SELECT name FROM sqlite_master WHERE type = 'table' AND name = 'Links';");
        Assert.Equal("Links", tableName);

        var uniqueCodeIndexExists = await GetScalarAsync(
            connection,
            """
            SELECT EXISTS (
                SELECT 1
                FROM pragma_index_list('Links')
                WHERE "unique" = 1
                  AND name = 'IX_Links_Code'
            );
            """);
        Assert.Equal(1L, uniqueCodeIndexExists);

        dbContext.Links.AddRange(
            new Link { Code = "DUPL", TargetUrl = "https://example.com", CreatedAtUtc = DateTime.UtcNow },
            new Link { Code = "DUPL", TargetUrl = "https://example.org", CreatedAtUtc = DateTime.UtcNow });

        await Assert.ThrowsAsync<DbUpdateException>(() => dbContext.SaveChangesAsync());
    }

    private static async Task<object?> GetScalarAsync(SqliteConnection connection, string sql)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;

        return await command.ExecuteScalarAsync();
    }
}
