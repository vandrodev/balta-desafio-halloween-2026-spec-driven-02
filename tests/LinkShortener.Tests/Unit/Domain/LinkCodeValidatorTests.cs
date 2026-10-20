using LinkShortener.Application.Domain.Links;

namespace LinkShortener.Tests.Unit.Domain;

public sealed class LinkCodeValidatorTests
{
    private readonly LinkCodeValidator _validator = new();

    [Theory]
    [InlineData("ABCD")]
    [InlineData("AZ19")]
    [InlineData("ABCDEFGHIJKLMNOPQRST")]
    public void Validate_AcceptsCodesWithinLengthAndAlphabet(string code)
    {
        var result = _validator.Validate(code);

        Assert.True(result.IsValid);
        Assert.Null(result.ErrorMessage);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC")]
    [InlineData("ABCDEFGHIJKLMNOPQRSTU")]
    public void Validate_RejectsCodesOutsideLengthRange(string code)
    {
        var result = _validator.Validate(code);

        Assert.False(result.IsValid);
        Assert.Equal("O código personalizado deve ter entre 4 e 20 caracteres.", result.ErrorMessage);
    }

    [Theory]
    [InlineData("ABC0")]
    [InlineData("abc1")]
    [InlineData("AB C")]
    [InlineData("AB-C")]
    [InlineData("AB_C")]
    public void Validate_RejectsCharactersOutsideAllowedAlphabet(string code)
    {
        var result = _validator.Validate(code);

        Assert.False(result.IsValid);
        Assert.Equal(
            "O código personalizado deve conter somente letras maiúsculas de A a Z e números de 1 a 9.",
            result.ErrorMessage);
    }
}
