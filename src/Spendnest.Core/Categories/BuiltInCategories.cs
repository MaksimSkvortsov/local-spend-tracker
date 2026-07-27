namespace Spendnest.Core.Categories;

/// <summary>
/// Provides the complete built-in category list for the MVP.
/// </summary>
public static class BuiltInCategories
{
    public static IReadOnlyList<BuiltInCategory> All { get; } =
    [
        new(BuiltInCategoryIds.Groceries, "Groceries", 10),
        new(BuiltInCategoryIds.RestaurantsAndCoffee, "Restaurants & Coffee", 20),
        new(BuiltInCategoryIds.Transportation, "Transportation", 30),
        new(BuiltInCategoryIds.Shopping, "Shopping", 40),
        new(BuiltInCategoryIds.Entertainment, "Entertainment", 50),
        new(BuiltInCategoryIds.Travel, "Travel", 60),
        new(BuiltInCategoryIds.Healthcare, "Healthcare", 70),
        new(BuiltInCategoryIds.Utilities, "Utilities", 80),
        new(BuiltInCategoryIds.Subscriptions, "Subscriptions", 90),
        new(BuiltInCategoryIds.Insurance, "Insurance", 100),
        new(BuiltInCategoryIds.PersonalCare, "Personal Care", 110),
        new(BuiltInCategoryIds.FeesAndCharges, "Fees & Charges", 120),
        new(BuiltInCategoryIds.CreditCardPayment, "Credit Card Payment", 130),
        new(BuiltInCategoryIds.Other, "Other", 150)
    ];
}
