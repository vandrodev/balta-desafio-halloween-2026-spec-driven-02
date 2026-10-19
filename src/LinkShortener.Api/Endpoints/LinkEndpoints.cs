using LinkShortener.Application.Links;

namespace LinkShortener.Api.Endpoints;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/links", CreateAsync)
            .WithName("CreateLink")
            .Produces<CreateLinkResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateLinkRequest request,
        LinkService linkService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await linkService.CreateAsync(request.TargetUrl, cancellationToken);
        if (!result.Succeeded)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Requisição inválida",
                detail: result.ErrorMessage);
        }

        var code = result.Link!.Code;
        var shortUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{code}";
        var response = new CreateLinkResponse(shortUrl, code);

        return Results.Created(shortUrl, response);
    }

    public sealed record CreateLinkRequest(string? TargetUrl);

    public sealed record CreateLinkResponse(string ShortUrl, string Code);
}
