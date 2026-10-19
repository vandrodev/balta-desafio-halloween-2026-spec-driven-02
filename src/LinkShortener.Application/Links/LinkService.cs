namespace LinkShortener.Application.Links;

public sealed class LinkService(
    ILinkRepository linkRepository,
    LinkCodeGenerator codeGenerator,
    TargetUrlValidator targetUrlValidator,
    LinkCodeValidator linkCodeValidator)
{
    public async Task<CreateLinkResult> CreateAsync(
        string? targetUrl,
        string? requestedCode = null,
        CancellationToken cancellationToken = default)
    {
        var validation = await targetUrlValidator.ValidateAsync(targetUrl, cancellationToken);
        if (!validation.IsValid)
        {
            return CreateLinkResult.InvalidTargetUrl(validation.ErrorMessage!);
        }

        if (requestedCode is not null)
        {
            var codeValidation = linkCodeValidator.Validate(requestedCode);
            if (!codeValidation.IsValid)
            {
                return CreateLinkResult.InvalidCode(codeValidation.ErrorMessage!);
            }

            if (await linkRepository.CodeExistsAsync(requestedCode, cancellationToken))
            {
                return CreateLinkResult.CodeAlreadyExists();
            }

            try
            {
                var link = CreateLink(requestedCode, targetUrl!);
                await linkRepository.AddAsync(link, cancellationToken);
                return CreateLinkResult.Success(link);
            }
            catch (LinkCodeAlreadyExistsException)
            {
                return CreateLinkResult.CodeAlreadyExists();
            }
        }

        while (true)
        {
            var code = await codeGenerator.GenerateUniqueAsync(
                linkRepository.CodeExistsAsync,
                cancellationToken);

            try
            {
                var link = CreateLink(code, targetUrl!);
                await linkRepository.AddAsync(link, cancellationToken);
                return CreateLinkResult.Success(link);
            }
            catch (LinkCodeAlreadyExistsException)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
        }
    }

    public async Task<LinkRedirectResult> GetRedirectAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var accessedAtUtc = DateTime.UtcNow;
        var accessCountIncremented = await linkRepository.TryIncrementAccessCountAsync(
            code,
            accessedAtUtc,
            cancellationToken);

        if (!accessCountIncremented)
        {
            var unavailableLink = await linkRepository.FindByCodeAsync(code, cancellationToken);
            if (unavailableLink is null)
            {
                return LinkRedirectResult.NotFound();
            }

            return LinkRedirectResult.Expired();
        }

        var link = await linkRepository.FindByCodeIfNotExpiredAsync(
            code,
            accessedAtUtc,
            cancellationToken);
        if (link is null)
        {
            var existingLink = await linkRepository.FindByCodeAsync(code, cancellationToken);
            return existingLink is null
                ? LinkRedirectResult.NotFound()
                : LinkRedirectResult.Expired();
        }

        return LinkRedirectResult.Success(link.TargetUrl);
    }

    public async Task<LinkStatisticsResult> GetStatisticsAsync(
        string code,
        CancellationToken cancellationToken = default)
    {
        var accessCount = await linkRepository.GetAccessCountByCodeAsync(code, cancellationToken);
        return accessCount.HasValue
            ? LinkStatisticsResult.Success(accessCount.Value)
            : LinkStatisticsResult.NotFound();
    }

    private static Link CreateLink(string code, string targetUrl)
    {
        return new Link
        {
            Code = code,
            TargetUrl = targetUrl,
            CreatedAtUtc = DateTime.UtcNow,
            AccessCount = 0
        };
    }
}
