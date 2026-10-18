namespace LinkShortener.Application.Links;

public sealed class Link
{
    public int Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public string TargetUrl { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? ExpiresAtUtc { get; set; }

    public long AccessCount { get; set; }
}
