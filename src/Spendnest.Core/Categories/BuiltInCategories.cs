namespace Spendnest.Core.Categories;

/// <summary>
/// Provides the complete built-in category list for the MVP.
/// </summary>
public static class BuiltInCategories
{
    public static IReadOnlyList<BuiltInCategory> All { get; } =
    [
        new(BuiltInCategoryCodes.Groceries, "Groceries", 10),
        new(BuiltInCategoryCodes.RestaurantsAndCoffee, "Restaurants & Coffee", 20),
        new(BuiltInCategoryCodes.Transportation, "Transportation", 30),
        new(BuiltInCategoryCodes.Shopping, "Shopping", 40),
        new(BuiltInCategoryCodes.Entertainment, "Entertainment", 50),
        new(BuiltInCategoryCodes.Travel, "Travel", 60),
        new(BuiltInCategoryCodes.Healthcare, "Healthcare", 70),
        new(BuiltInCategoryCodes.Utilities, "Utilities", 80),
        new(BuiltInCategoryCodes.Subscriptions, "Subscriptions", 90),
        new(BuiltInCategoryCodes.Insurance, "Insurance", 100),
        new(BuiltInCategoryCodes.PersonalCare, "Personal Care", 110),
        new(BuiltInCategoryCodes.FeesAndCharges, "Fees & Charges", 120),
        new(BuiltInCategoryCodes.CreditCardPayment, "Credit Card Payment", 130),
        new(BuiltInCategoryCodes.Refund, "Refund", 140),
        new(BuiltInCategoryCodes.Other, "Other", 150)
    ];
}
