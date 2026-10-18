using LinkShortener.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("LinkShortener")
    ?? throw new InvalidOperationException("A connection string named 'LinkShortener' is required.");

builder.Services.AddDbContext<LinkShortenerDbContext>(options =>
    options.UseSqlite(connectionString));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
    dbContext.Database.Migrate();
}

app.Run();
