using LinkShortener.Application.Links;

namespace LinkShortener.Api.Endpoints;

public static class LinkEndpoints
{
    public static IEndpointRouteBuilder MapLinkEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/links", CreateAsync)
            .WithName("CreateLink")
            .Produces<CreateLinkResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status409Conflict);

        endpoints.MapGet("/{code}", RedirectAsync)
            .WithName("RedirectLink")
            .Produces(StatusCodes.Status302Found)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status410Gone);

        return endpoints;
    }

    private static async Task<IResult> CreateAsync(
        CreateLinkRequest request,
        LinkService linkService,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        var result = await linkService.CreateAsync(
            request.TargetUrl,
            request.Code,
            cancellationToken);
        if (!result.Succeeded)
        {
            var statusCode = result.Failure == CreateLinkFailure.CodeAlreadyExists
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

            return Results.Problem(
                statusCode: statusCode,
                title: "Requisição inválida",
                detail: result.ErrorMessage);
        }

        var code = result.Link!.Code;
        var shortUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}/{code}";
        var response = new CreateLinkResponse(shortUrl, code);

        return Results.Created(shortUrl, response);
    }

    private static async Task<IResult> RedirectAsync(
        string code,
        LinkService linkService,
        CancellationToken cancellationToken)
    {
        var result = await linkService.GetRedirectAsync(code, cancellationToken);
        if (result.Failure is LinkRedirectFailure.NotFound)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Link não encontrado");
        }

        if (result.Failure is LinkRedirectFailure.Expired)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status410Gone,
                title: "Link expirado");
        }

        return Results.Redirect(result.TargetUrl!);
    }

    public sealed record CreateLinkRequest(string? TargetUrl, string? Code = null);

    public sealed record CreateLinkResponse(string ShortUrl, string Code);
}
