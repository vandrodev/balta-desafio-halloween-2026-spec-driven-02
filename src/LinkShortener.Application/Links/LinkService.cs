namespace LinkShortener.Application.Links;

public sealed class LinkService(
    ILinkRepository linkRepository,
    LinkCodeGenerator codeGenerator,
    TargetUrlValidator targetUrlValidator)
{
    public async Task<CreateLinkResult> CreateAsync(
        string? targetUrl,
        CancellationToken cancellationToken = default)
    {
        var validation = await targetUrlValidator.ValidateAsync(targetUrl, cancellationToken);
        if (!validation.IsValid)
        {
            return CreateLinkResult.Failure(validation.ErrorMessage!);
        }

        var code = await codeGenerator.GenerateUniqueAsync(
            linkRepository.CodeExistsAsync,
            cancellationToken);

        var link = new Link
        {
            Code = code,
            TargetUrl = targetUrl!,
            CreatedAtUtc = DateTime.UtcNow,
            AccessCount = 0
        };

        await linkRepository.AddAsync(link, cancellationToken);

        return CreateLinkResult.Success(link);
    }
}
