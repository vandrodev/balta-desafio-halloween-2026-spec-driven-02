namespace LinkShortener.Application.Domain.Links;

public sealed class LinkExpirationPolicy
{
    public LinkExpirationResult Validate(
        DateTimeOffset? requestedExpiresAtUtc,
        DateTimeOffset currentUtcTime)
    {
        if (requestedExpiresAtUtc is null)
        {
            return LinkExpirationResult.Valid(null);
        }

        var requestedUtcDate = requestedExpiresAtUtc.Value.UtcDateTime.Date;
        var currentUtcDate = currentUtcTime.UtcDateTime.Date;

        if (requestedUtcDate < currentUtcDate)
        {
            return LinkExpirationResult.Invalid("A data de expiração não pode estar no passado.");
        }

        var lastValidInstantUtc = requestedUtcDate == DateTime.MaxValue.Date
            ? DateTime.MaxValue
            : requestedUtcDate.AddDays(1).AddTicks(-1);

        return LinkExpirationResult.Valid(
            DateTime.SpecifyKind(lastValidInstantUtc, DateTimeKind.Utc));
    }
}
