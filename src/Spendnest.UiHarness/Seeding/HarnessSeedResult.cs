namespace Spendnest.UiHarness.Seeding;

/// <summary>
/// What seeding created, so browser tests can assert ids and totals without repeating the catalog.
/// </summary>
public sealed record HarnessSeedResult(bool SettingsConfigured, IReadOnlyList<HarnessSeedCardResult> Cards);

public sealed record HarnessSeedCardResult(Guid Id, string Name, IReadOnlyList<HarnessSeedImportResult> Imports);

/// <summary>
/// One seeded import. <see cref="SpendingTotal"/> sums amounts in categories that count as spending,
/// so refunds reduce it and card payments and income are left out.
/// </summary>
public sealed record HarnessSeedImportResult(
    Guid Id,
    string FileName,
    int TransactionCount,
    int NeedsReviewCount,
    decimal SpendingTotal);
