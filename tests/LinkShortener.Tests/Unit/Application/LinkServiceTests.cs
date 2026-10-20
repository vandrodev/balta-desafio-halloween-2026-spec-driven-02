using LinkShortener.Application.Domain.Links;
using LinkShortener.Application.Links.Contracts;
using LinkShortener.Application.Links.Results;
using LinkShortener.Application.Links.Services;

namespace LinkShortener.Tests.Unit.Application;

public sealed class LinkServiceTests
{
    private static readonly DateTimeOffset CurrentUtcTime =
        new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_RejectsCustomCodeAlreadyInUse()
    {
        var repository = new TestLinkRepository();
        repository.Links.Add(new Link
        {
            Code = "TAKEN1",
            TargetUrl = "https://example.com/existing",
            CreatedAtUtc = CurrentUtcTime.UtcDateTime
        });
        var service = CreateService(repository);

        var result = await service.CreateAsync(
            "https://example.com/new",
            "TAKEN1");

        Assert.Equal(CreateLinkFailure.CodeAlreadyExists, result.Failure);
        Assert.Single(repository.Links);
    }

    [Fact]
    public async Task GetRedirectAsync_DoesNotRedirectOrIncrementExpiredLink()
    {
        var repository = new TestLinkRepository();
        repository.Links.Add(new Link
        {
            Code = "EXPRD1",
            TargetUrl = "https://example.com/expired",
            CreatedAtUtc = CurrentUtcTime.UtcDateTime.AddDays(-1),
            ExpiresAtUtc = CurrentUtcTime.UtcDateTime.AddTicks(-1),
            AccessCount = 4
        });
        var service = CreateService(repository);

        var result = await service.GetRedirectAsync("EXPRD1");

        Assert.Equal(LinkRedirectFailure.Expired, result.Failure);
        Assert.Equal(4, repository.Links.Single().AccessCount);
        Assert.Equal(1, repository.IncrementAttempts);
    }

    [Fact]
    public async Task GetRedirectAsync_OnlyIncrementsAccessCountForValidAccesses()
    {
        var repository = new TestLinkRepository();
        repository.Links.Add(new Link
        {
            Code = "VALID1",
            TargetUrl = "https://example.com/valid",
            CreatedAtUtc = CurrentUtcTime.UtcDateTime,
            AccessCount = 4
        });
        var service = CreateService(repository);

        var firstResult = await service.GetRedirectAsync("VALID1");
        var countAfterFirstAccess = repository.Links.Single().AccessCount;
        var secondResult = await service.GetRedirectAsync("VALID1");
        var countAfterSecondAccess = repository.Links.Single().AccessCount;

        Assert.True(firstResult.Succeeded);
        Assert.True(secondResult.Succeeded);
        Assert.Equal(5, countAfterFirstAccess);
        Assert.Equal(6, countAfterSecondAccess);
        Assert.Equal(2, repository.IncrementAttempts);
    }

    private static LinkService CreateService(TestLinkRepository repository)
    {
        return new LinkService(
            repository,
            new LinkCodeGenerator(),
            new TargetUrlValidator(new AccessibleUrlChecker()),
            new LinkCodeValidator(),
            new LinkExpirationPolicy(),
            new FixedTimeProvider(CurrentUtcTime));
    }

    private sealed class TestLinkRepository : ILinkRepository
    {
        public List<Link> Links { get; } = [];

        public int IncrementAttempts { get; private set; }

        public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(Links.Any(link => link.Code == code));
        }

        public Task<Link?> FindByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(Links.SingleOrDefault(link => link.Code == code));
        }

        public Task<bool> TryIncrementAccessCountAsync(
            string code,
            DateTime accessedAtUtc,
            CancellationToken cancellationToken)
        {
            IncrementAttempts++;
            var link = Links.SingleOrDefault(candidate => candidate.Code == code);
            if (link is null || link.ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc < accessedAtUtc)
            {
                return Task.FromResult(false);
            }

            link.AccessCount++;
            return Task.FromResult(true);
        }

        public Task<Link?> FindByCodeIfNotExpiredAsync(
            string code,
            DateTime accessedAtUtc,
            CancellationToken cancellationToken)
        {
            var link = Links.SingleOrDefault(candidate => candidate.Code == code);
            if (link?.ExpiresAtUtc is { } expiresAtUtc && expiresAtUtc < accessedAtUtc)
            {
                link = null;
            }

            return Task.FromResult(link);
        }

        public Task<long?> GetAccessCountByCodeAsync(string code, CancellationToken cancellationToken)
        {
            return Task.FromResult(
                Links.Where(link => link.Code == code)
                    .Select(link => (long?)link.AccessCount)
                    .SingleOrDefault());
        }

        public Task AddAsync(Link link, CancellationToken cancellationToken)
        {
            Links.Add(link);
            return Task.CompletedTask;
        }
    }

    private sealed class AccessibleUrlChecker : IUrlAccessibilityChecker
    {
        public Task<bool> IsPubliclyAccessibleAsync(Uri targetUri, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
