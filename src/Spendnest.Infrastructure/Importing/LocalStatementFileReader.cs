using System.Security.Cryptography;
using Spendnest.Application.Importing;

namespace Spendnest.Infrastructure.Importing;

public sealed class LocalStatementFileReader : IStatementFileReader
{
    public async Task<StatementFileReadResult> OpenReadAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            throw new ArgumentException("File path is required.", nameof(filePath));
        }

        var fileHash = await ComputeFileHashAsync(filePath, cancellationToken).ConfigureAwait(false);

        return new StatementFileReadResult(
            filePath,
            Path.GetFileName(filePath),
            fileHash,
            File.OpenRead(filePath));
    }

    private static async Task<string> ComputeFileHashAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hashBytes = await SHA256.HashDataAsync(stream, cancellationToken).ConfigureAwait(false);
        return Convert.ToHexString(hashBytes);
    }
}
