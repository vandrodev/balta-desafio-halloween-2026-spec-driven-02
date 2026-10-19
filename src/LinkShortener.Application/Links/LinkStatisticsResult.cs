namespace LinkShortener.Application.Links;

public sealed class LinkStatisticsResult
{
    private LinkStatisticsResult(long? accessCount)
    {
        AccessCount = accessCount;
    }

    public long? AccessCount { get; }

    public bool Found => AccessCount.HasValue;

    public static LinkStatisticsResult Success(long accessCount) => new(accessCount);

    public static LinkStatisticsResult NotFound() => new(null);
}
