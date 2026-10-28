using System.Security.Cryptography;
using System.Text;
using Spendnest.Core.Accounts;
using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Importing;
using Spendnest.Core.Transactions;

namespace Spendnest.UiHarness.Seeding;

/// <summary>
/// The rows seeding writes, built only from the chosen cards: no clock, no randomness, and ids derived
/// from names, so the same cards always produce the same data.
/// </summary>
public sealed record HarnessSeedData(
    IReadOnlyList<CardAccount> Cards,
    IReadOnlyList<StatementImport> Imports,
    IReadOnlyList<Transaction> Transactions,
    IReadOnlyList<TransactionCategoryAssignment> Assignments,
    IReadOnlyList<CategoryRule> Rules)
{
    /// <summary>December 2025 and January 2026, one completed import per card each.</summary>
    public static IReadOnlyList<SeedMonth> Months { get; } =
    [
        new(2025, 12, AmountFactor: 1.00m),
        new(2026, 1, AmountFactor: 0.90m)
    ];

    /// <summary>Added whenever a card is seeded, so the Rules page has content.</summary>
    public static SeedRule UberRule { get; } = new("UBER", BuiltInCategoryIds.Transportation);

    public static HarnessSeedData Build(IReadOnlyList<SeedCard> seedCards)
    {
        var cards = new List<CardAccount>();
        var imports = new List<StatementImport>();
        var transactions = new List<Transaction>();
        var assignments = new List<TransactionCategoryAssignment>();
        var rules = new List<CategoryRule>();

        if (seedCards.Count > 0)
        {
            rules.Add(new CategoryRule
            {
                Id = CreateId("rule", UberRule.Pattern),
                Pattern = UberRule.Pattern,
                CategoryId = UberRule.CategoryId,
                MatchType = CategoryRuleMatchType.Contains
            });
        }

        foreach (var seedCard in seedCards)
        {
            var card = new CardAccount
            {
                Id = CreateId("card", seedCard.Key),
                Name = seedCard.Name,
                CreatedAtUtc = new DateTimeOffset(2025, 12, 1, 9, 0, 0, TimeSpan.Zero)
            };
            cards.Add(card);

            foreach (var month in Months)
            {
                var statementImport = CreateImport(seedCard, card, month);
                imports.Add(statementImport);

                var rowNumber = 0;
                foreach (var template in seedCard.MonthlyTransactions)
                {
                    rowNumber++;
                    var transaction = new Transaction
                    {
                        Id = CreateId("transaction", $"{seedCard.Key}/{month.Key}/{rowNumber}"),
                        CardAccountId = card.Id,
                        StatementImportId = statementImport.Id,
                        PostedDate = new DateOnly(month.Year, month.Month, template.Day),
                        OriginalDescription = template.Description,
                        Amount = month.Scale(template.Amount),
                        SourceRowNumber = rowNumber,
                        ImportedAtUtc = statementImport.CompletedAtUtc!.Value
                    };
                    transactions.Add(transaction);
                    assignments.Add(CreateAssignment(transaction, template, statementImport.CompletedAtUtc.Value));
                }

                statementImport.ParsedRowCount = rowNumber;
                statementImport.SavedTransactionCount = rowNumber;
            }
        }

        return new HarnessSeedData(cards, imports, transactions, assignments, rules);
    }

    public HarnessSeedResult ToResult(bool settingsConfigured)
    {
        var assignmentByTransaction = Assignments.ToDictionary(assignment => assignment.TransactionId);
        var cardResults = Cards
            .Select(card => new HarnessSeedCardResult(
                card.Id,
                card.Name,
                Imports
                    .Where(statementImport => statementImport.CardAccountId == card.Id)
                    .Select(statementImport => CreateImportResult(statementImport, assignmentByTransaction))
                    .ToArray()))
            .ToArray();

        return new HarnessSeedResult(settingsConfigured, cardResults);
    }

    private HarnessSeedImportResult CreateImportResult(
        StatementImport statementImport,
        IReadOnlyDictionary<Guid, TransactionCategoryAssignment> assignmentByTransaction)
    {
        var importTransactions = Transactions
            .Where(transaction => transaction.StatementImportId == statementImport.Id)
            .Select(transaction => (Transaction: transaction, Assignment: assignmentByTransaction[transaction.Id]))
            .ToArray();

        return new HarnessSeedImportResult(
            statementImport.Id,
            statementImport.FileName,
            importTransactions.Length,
            importTransactions.Count(item => item.Assignment.NeedsReview),
            importTransactions
                .Where(item => BuiltInCategories.CountsAsSpending(item.Assignment.CategoryId))
                .Sum(item => item.Transaction.Amount));
    }

    private static StatementImport CreateImport(SeedCard seedCard, CardAccount card, SeedMonth month)
    {
        var fileName = $"{seedCard.Key}-{month.Key}.csv";
        var completedAtUtc = month.ImportedAtUtc;

        return new StatementImport
        {
            Id = CreateId("import", $"{seedCard.Key}/{month.Key}"),
            CardAccountId = card.Id,
            FilePath = Path.Combine("seed", fileName),
            FileName = fileName,
            FileHash = $"seed-{seedCard.Key}-{month.Key}",
            Status = StatementImportStatus.Completed,
            StartedAtUtc = completedAtUtc.AddMinutes(-1),
            CompletedAtUtc = completedAtUtc
        };
    }

    private static TransactionCategoryAssignment CreateAssignment(
        Transaction transaction,
        SeedTransactionTemplate template,
        DateTimeOffset updatedAtUtc)
    {
        var categorization = template switch
        {
            { NeedsReview: true } => (Source: CategorizationSource.Ai, Confidence: 0.45m,
                Explanation: "Seeded AI result with low confidence."),
            _ when transaction.OriginalDescription.Contains(UberRule.Pattern, StringComparison.OrdinalIgnoreCase) =>
                (Source: CategorizationSource.LocalRules, Confidence: 1.00m,
                    Explanation: $"Matched rule '{UberRule.Pattern}'."),
            _ => (Source: CategorizationSource.Ai, Confidence: 0.92m, Explanation: "Seeded AI result.")
        };

        return new TransactionCategoryAssignment
        {
            TransactionId = transaction.Id,
            CategoryId = template.CategoryId,
            Confidence = categorization.Confidence,
            NeedsReview = template.NeedsReview,
            Source = categorization.Source,
            Explanation = categorization.Explanation,
            UpdatedAtUtc = updatedAtUtc
        };
    }

    private static Guid CreateId(string kind, string name)
    {
        var hash = MD5.HashData(Encoding.UTF8.GetBytes($"spendnest-seed/{kind}/{name}"));
        return new Guid(hash);
    }
}

public sealed record SeedMonth(int Year, int Month, decimal AmountFactor)
{
    public string Key => $"{Year:D4}-{Month:D2}";

    /// <summary>Statements are imported on the 2nd of the following month.</summary>
    public DateTimeOffset ImportedAtUtc =>
        new DateTimeOffset(Year, Month, 2, 9, 0, 0, TimeSpan.Zero).AddMonths(1);

    public decimal Scale(decimal amount)
    {
        return Math.Round(amount * AmountFactor, 2, MidpointRounding.AwayFromZero);
    }
}

public sealed record SeedRule(string Pattern, int CategoryId);
