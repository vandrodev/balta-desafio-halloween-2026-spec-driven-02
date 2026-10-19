using LinkShortener.Application.Links;

namespace LinkShortener.Tests;

public sealed class LinkExpirationPolicyTests
{
    private readonly LinkExpirationPolicy _policy = new();
    private static readonly DateTimeOffset CurrentUtcTime =
        new(2026, 10, 18, 23, 59, 0, TimeSpan.Zero);

    [Fact]
    public void Validate_RejectsUtcDateBeforeCurrentUtcDate()
    {
        var result = _policy.Validate(
            new DateTimeOffset(2026, 10, 17, 23, 59, 0, TimeSpan.Zero),
            CurrentUtcTime);

        Assert.False(result.IsValid);
        Assert.Equal("A data de expiração não pode estar no passado.", result.ErrorMessage);
    }

    [Fact]
    public void Validate_UsesLastInstantOfRequestedUtcDateAsInclusiveExpiry()
    {
        var result = _policy.Validate(
            new DateTimeOffset(2026, 10, 18, 0, 1, 0, TimeSpan.Zero),
            CurrentUtcTime);

        Assert.True(result.IsValid);
        Assert.Equal(
            new DateTime(2026, 10, 18, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999),
            result.ExpiresAtUtc);
    }

    [Fact]
    public void Validate_UsesUtcCalendarDateForValuesWithNonZeroOffset()
    {
        var result = _policy.Validate(
            new DateTimeOffset(2026, 10, 19, 1, 0, 0, TimeSpan.FromHours(2)),
            CurrentUtcTime);

        Assert.True(result.IsValid);
        Assert.Equal(
            new DateTime(2026, 10, 18, 23, 59, 59, DateTimeKind.Utc).AddTicks(9_999_999),
            result.ExpiresAtUtc);
    }

    [Fact]
    public void Validate_AllowsNoExpiration()
    {
        var result = _policy.Validate(null, CurrentUtcTime);

        Assert.True(result.IsValid);
        Assert.Null(result.ExpiresAtUtc);
    }
}
