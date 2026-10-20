using LinkShortener.Api.Contracts;
using LinkShortener.Application.Links.Results;
using LinkShortener.Application.Links.Services;

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

        endpoints.MapGet("/api/links/{code}/stats", GetStatisticsAsync)
            .WithName("GetLinkStatistics")
            .Produces<LinkStatisticsResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

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
            request.ExpiresAtUtc,
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

    private static async Task<IResult> GetStatisticsAsync(
        string code,
        LinkService linkService,
        CancellationToken cancellationToken)
    {
        var result = await linkService.GetStatisticsAsync(code, cancellationToken);
        if (!result.Found)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Link não encontrado");
        }

        return Results.Ok(new LinkStatisticsResponse(result.AccessCount!.Value));
    }

}
