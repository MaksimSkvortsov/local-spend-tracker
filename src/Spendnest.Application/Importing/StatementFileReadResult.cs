namespace Spendnest.Application.Importing;

public sealed class StatementFileReadResult : IAsyncDisposable
{
    public StatementFileReadResult(
        string filePath,
        string fileName,
        string fileHash,
        Stream content)
    {
        FilePath = filePath;
        FileName = fileName;
        FileHash = fileHash;
        Content = content;
    }

    public string FilePath { get; }

    public string FileName { get; }

    public string FileHash { get; }

    public Stream Content { get; }

    public async ValueTask DisposeAsync()
    {
        await Content.DisposeAsync().ConfigureAwait(false);
    }
}
