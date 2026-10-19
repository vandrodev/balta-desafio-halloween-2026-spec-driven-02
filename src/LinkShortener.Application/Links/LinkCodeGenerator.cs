using System.Security.Cryptography;

namespace LinkShortener.Application.Links;

public sealed class LinkCodeGenerator
{
    private const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ123456789";
    private const int CodeLength = 7;

    public string Generate()
    {
        return string.Create(
            CodeLength,
            Alphabet,
            static (code, alphabet) =>
            {
                for (var index = 0; index < code.Length; index++)
                {
                    code[index] = alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)];
                }
            });
    }

    public async Task<string> GenerateUniqueAsync(
        Func<string, CancellationToken, Task<bool>> codeExistsAsync,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(codeExistsAsync);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var code = Generate();

            if (!await codeExistsAsync(code, cancellationToken))
            {
                return code;
            }
        }
    }
}
