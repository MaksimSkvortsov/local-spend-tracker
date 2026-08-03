namespace Spendnest.Core.Importing;

/// <summary>
/// Records one statement file import attempt and the counts produced by it.
/// </summary>
public sealed class StatementImport
{
    public Guid Id { get; init; } = Guid.NewGuid();

    public Guid CardAccountId { get; init; }

    public string FilePath { get; init; } = string.Empty;

    public string FileName { get; init; } = string.Empty;

    public string FileHash { get; init; } = string.Empty;

    public StatementImportStatus Status { get; set; } = StatementImportStatus.Pending;

    public int ParsedRowCount { get; set; }

    public int SavedTransactionCount { get; set; }

    public int SkippedDuplicateTransactionCount { get; set; }

    public int FailedRowCount { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTimeOffset StartedAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? CompletedAtUtc { get; set; }
}
