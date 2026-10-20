using LinkShortener.Application.Domain.Links;

namespace LinkShortener.Tests.Unit.Domain;

public sealed class LinkCodeGeneratorTests
{
    private const string AllowedCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";

    [Fact]
    public void Generate_ReturnsSevenCharactersFromAllowedAlphabet()
    {
        var generator = new LinkCodeGenerator();

        for (var attempt = 0; attempt < 100; attempt++)
        {
            var code = generator.Generate();

            Assert.Equal(7, code.Length);
            Assert.All(code, character => Assert.Contains(character, AllowedCharacters));
        }
    }

    [Fact]
    public async Task GenerateUniqueAsync_RetriesWhenGeneratedCodeAlreadyExists()
    {
        var generator = new LinkCodeGenerator();
        var checkedCodes = new List<string>();

        var uniqueCode = await generator.GenerateUniqueAsync(
            (code, _) =>
            {
                checkedCodes.Add(code);
                return Task.FromResult(checkedCodes.Count == 1);
            });

        Assert.Equal(2, checkedCodes.Count);
        Assert.Equal(checkedCodes[1], uniqueCode);
        Assert.NotEqual(checkedCodes[0], uniqueCode);
    }
}
