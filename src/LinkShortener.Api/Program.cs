using LinkShortener.Application.Links;
using LinkShortener.Api.Endpoints;
using LinkShortener.Infrastructure.Persistence;
using LinkShortener.Infrastructure.Links;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("LinkShortener")
    ?? throw new InvalidOperationException("A connection string named 'LinkShortener' is required.");

builder.Services.AddDbContext<LinkShortenerDbContext>(options =>
    options.UseSqlite(connectionString));
builder.Services.AddSingleton<LinkRedirectCache>();
builder.Services.AddSingleton<IHostAddressResolver, SystemHostAddressResolver>();
builder.Services.AddSingleton<IUrlAccessibilityChecker, PublicUrlAccessibilityChecker>();
builder.Services.AddSingleton<LinkCodeGenerator>();
builder.Services.AddTransient<TargetUrlValidator>();
builder.Services.AddSingleton<LinkCodeValidator>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<LinkExpirationPolicy>();
builder.Services.AddScoped<ILinkRepository, LinkRepository>();
builder.Services.AddScoped<LinkService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<LinkShortenerDbContext>();
    dbContext.Database.Migrate();
}

app.MapLinkEndpoints();

app.Run();

public partial class Program { }
