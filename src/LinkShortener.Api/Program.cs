using LinkShortener.Api.Endpoints;
using LinkShortener.Infrastructure.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddLinkShortenerInfrastructure(builder.Configuration);

var app = builder.Build();

app.Services.InitializeLinkShortenerDatabase();
app.MapLinkEndpoints();

app.Run();

public partial class Program { }
