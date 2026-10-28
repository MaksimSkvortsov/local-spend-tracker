using Spendnest.Core.Categories;

namespace Spendnest.UiHarness.Seeding;

/// <summary>
/// One monthly transaction a seeded card gets. <see cref="Merchant"/> is the keyword the fake AI
/// categorizer matches, so imported statements with the same merchants categorize the same way.
/// </summary>
public sealed record SeedTransactionTemplate(
    int Day,
    string Description,
    string Merchant,
    decimal Amount,
    int CategoryId,
    bool NeedsReview = false);

public sealed record SeedCard(string Key, string Name, IReadOnlyList<SeedTransactionTemplate> MonthlyTransactions);

/// <summary>
/// The fixed cards browser tests can seed. Spending is positive and money in is negative, as in
/// imported statements. Each card has a refund, a payment or deposit, and one transaction needing review.
/// </summary>
public static class SeedCardCatalog
{
    public static SeedCard ChaseSapphire { get; } = new("sapphire", "Chase Sapphire",
    [
        new(2, "WHOLE FOODS MARKET #10234", "WHOLE FOODS", 86.40m, BuiltInCategoryIds.Groceries),
        new(4, "STARBUCKS STORE 08812", "STARBUCKS", 6.75m, BuiltInCategoryIds.RestaurantsAndCoffee),
        new(6, "UBER *TRIP HELP.UBER.COM", "UBER", 24.30m, BuiltInCategoryIds.Transportation),
        new(9, "AMAZON.COM*MK2LP0 AMZN.COM/BILL", "AMAZON", 54.99m, BuiltInCategoryIds.Shopping),
        new(11, "NETFLIX.COM", "NETFLIX", 15.49m, BuiltInCategoryIds.Subscriptions),
        new(14, "DELTA AIR LINES 0062", "DELTA AIR", 412.60m, BuiltInCategoryIds.Travel),
        new(17, "OLIVE GARDEN 1187", "OLIVE GARDEN", 48.20m, BuiltInCategoryIds.RestaurantsAndCoffee),
        new(20, "AMAZON.COM REFUND", "AMAZON", -19.99m, BuiltInCategoryIds.Shopping),
        new(23, "SQ *MARKET STALL 22", "SQ *MARKET STALL", 31.00m, BuiltInCategoryIds.Other, NeedsReview: true),
        new(26, "AUTOPAY PAYMENT - THANK YOU", "AUTOPAY PAYMENT", -1250.00m, BuiltInCategoryIds.CreditCardPayment)
    ]);

    public static SeedCard CapOneDebit { get; } = new("capone", "CapOne Debit",
    [
        new(1, "PAYROLL DEPOSIT ACME CORP", "PAYROLL DEPOSIT", -3200.00m, BuiltInCategoryIds.IncomeAndTransfers),
        new(3, "TRADER JOE'S #552", "TRADER JOE", 64.18m, BuiltInCategoryIds.Groceries),
        new(5, "SHELL OIL 57442", "SHELL OIL", 45.10m, BuiltInCategoryIds.Transportation),
        new(8, "UBER *TRIP HELP.UBER.COM", "UBER", 18.60m, BuiltInCategoryIds.Transportation),
        new(10, "CVS/PHARMACY #0912", "CVS/PHARMACY", 22.37m, BuiltInCategoryIds.Healthcare),
        new(12, "SPOTIFY USA", "SPOTIFY", 11.99m, BuiltInCategoryIds.Subscriptions),
        new(15, "AMC THEATRES 2231", "AMC THEATRES", 32.50m, BuiltInCategoryIds.Entertainment),
        new(18, "GREAT CLIPS #4410", "GREAT CLIPS", 24.00m, BuiltInCategoryIds.PersonalCare),
        new(21, "TARGET REFUND", "TARGET", -15.00m, BuiltInCategoryIds.Shopping),
        new(24, "VENMO *J SMITH", "VENMO", 60.00m, BuiltInCategoryIds.Other, NeedsReview: true)
    ]);

    public static SeedCard BofaChecking { get; } = new("boa", "BofA Checking",
    [
        new(1, "DIRECT DEP EMPLOYER INC", "DIRECT DEP", -2800.00m, BuiltInCategoryIds.IncomeAndTransfers),
        new(2, "PG&E UTILITY BILL", "PG&E", 132.45m, BuiltInCategoryIds.Utilities),
        new(5, "COMCAST XFINITY", "COMCAST", 89.99m, BuiltInCategoryIds.Utilities),
        new(7, "GEICO AUTO INSURANCE", "GEICO", 118.00m, BuiltInCategoryIds.Insurance),
        new(9, "COSTCO WHSE #0423", "COSTCO", 156.32m, BuiltInCategoryIds.Groceries),
        new(13, "CHIPOTLE 2210", "CHIPOTLE", 14.85m, BuiltInCategoryIds.RestaurantsAndCoffee),
        new(16, "COURSERA.ORG", "COURSERA", 49.00m, BuiltInCategoryIds.Education),
        new(19, "MONTHLY SERVICE FEE", "SERVICE FEE", 12.00m, BuiltInCategoryIds.FeesAndCharges),
        new(22, "CHASE CREDIT CRD AUTOPAY", "CHASE CREDIT CRD", 1250.00m, BuiltInCategoryIds.CreditCardPayment),
        new(25, "ZELLE PAYMENT TO R LEE", "ZELLE", 75.00m, BuiltInCategoryIds.Other, NeedsReview: true),
        new(27, "COSTCO WHSE REFUND", "COSTCO", -20.00m, BuiltInCategoryIds.Groceries)
    ]);

    public static IReadOnlyList<SeedCard> All { get; } = [ChaseSapphire, CapOneDebit, BofaChecking];

    public static SeedCard? Find(string key)
    {
        return All.FirstOrDefault(card => card.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
