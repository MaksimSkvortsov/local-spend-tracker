namespace Spendnest.Core.Categories;

/// <summary>
/// Provides the complete built-in category list for the MVP.
/// </summary>
public static class BuiltInCategories
{
    public static IReadOnlyList<BuiltInCategory> All { get; } =
    [
        new(BuiltInCategoryIds.Groceries, "Groceries", 10, "#69c145"),
        new(BuiltInCategoryIds.RestaurantsAndCoffee, "Restaurants & Coffee", 20, "#009a55"),
        new(BuiltInCategoryIds.Transportation, "Transportation", 30, "#04ae5c"),
        new(BuiltInCategoryIds.Shopping, "Shopping", 40, "#006d36"),
        new(BuiltInCategoryIds.Entertainment, "Entertainment", 50, "#efa912"),
        new(BuiltInCategoryIds.Travel, "Travel", 60, "#005227"),
        new(BuiltInCategoryIds.Healthcare, "Healthcare", 70, "#7ecf55"),
        new(BuiltInCategoryIds.Utilities, "Utilities", 80, "#13a35a"),
        new(BuiltInCategoryIds.Subscriptions, "Subscriptions", 90, "#f0b21c"),
        new(BuiltInCategoryIds.Insurance, "Insurance", 100, "#b33b1a"),
        new(BuiltInCategoryIds.PersonalCare, "Personal Care", 110, "#46b83f"),
        new(BuiltInCategoryIds.FeesAndCharges, "Fees & Charges", 120, "#c24a0a"),
        new(BuiltInCategoryIds.CreditCardPayment, "Credit Card Payment", 130, "#c7cbc5"),
        new(BuiltInCategoryIds.Other, "Other", 150, "#e5e7e2")
    ];
}
