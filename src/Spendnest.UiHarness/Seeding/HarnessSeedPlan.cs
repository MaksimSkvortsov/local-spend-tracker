namespace Spendnest.UiHarness.Seeding;

/// <summary>
/// Body of <c>POST /harness/seed</c>: whether AI settings are configured, and which catalog cards to seed.
/// </summary>
public sealed record HarnessSeedRequest(bool? Settings, IReadOnlyList<string>? Cards);

/// <summary>
/// A validated seed request: the settings flag and the catalog cards in request order.
/// </summary>
public sealed record HarnessSeedPlan(bool Settings, IReadOnlyList<SeedCard> Cards)
{
    public static HarnessSeedPlan FromRequest(HarnessSeedRequest request)
    {
        var keys = request.Cards ?? [];
        var duplicateKeys = keys
            .GroupBy(key => key, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateKeys.Length > 0)
        {
            throw new ArgumentException($"Cards are listed more than once: {string.Join(", ", duplicateKeys)}.");
        }

        var cards = new List<SeedCard>();
        foreach (var key in keys)
        {
            var card = SeedCardCatalog.Find(key)
                ?? throw new ArgumentException(
                    $"Unknown card '{key}'. Known cards: {string.Join(", ", SeedCardCatalog.All.Select(known => known.Key))}.");
            cards.Add(card);
        }

        return new HarnessSeedPlan(request.Settings ?? false, cards);
    }
}
