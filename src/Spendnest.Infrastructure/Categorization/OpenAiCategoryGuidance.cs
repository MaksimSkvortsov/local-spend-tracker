using Spendnest.Core.Categories;

namespace Spendnest.Infrastructure.Categorization;

public static class OpenAiCategoryGuidance
{
    public static string Get(int categoryId)
    {
        return categoryId switch
        {
            BuiltInCategoryIds.Groceries => "Grocery stores, supermarkets, warehouse groceries, food markets, Costco/Trader Joe's/Giant/Wegmans/Harris Teeter/Food Lion/Whole Foods.",
            BuiltInCategoryIds.RestaurantsAndCoffee => "Restaurants, cafes, bakeries, bars, delivery services, DoorDash, Grubhub, Potbelly, pizza, sushi, coffee shops.",
            BuiltInCategoryIds.Transportation => "Gas, rideshare, taxis, parking, tolls, car service, Uber, Lyft, fuel stations.",
            BuiltInCategoryIds.Shopping => "Retail stores, Amazon purchases, Target, Home Depot, clothing, electronics, household goods.",
            BuiltInCategoryIds.Entertainment => "Concerts, movies, museums, parks, tickets, clubs, zoo, shows.",
            BuiltInCategoryIds.Travel => "Hotels, airfare, Airbnb, booking sites, rental cars, travel insurance, tourist transport.",
            BuiltInCategoryIds.Healthcare => "Doctors, pharmacies, medical, dental, vision, health services.",
            BuiltInCategoryIds.Utilities => "Water, electric, gas utilities, internet, phone, municipal services.",
            BuiltInCategoryIds.Subscriptions => "Recurring digital services, memberships, streaming, software subscriptions.",
            BuiltInCategoryIds.Insurance => "Insurance premiums and insurance providers.",
            BuiltInCategoryIds.PersonalCare => "Haircuts, barber, spa, massage, cosmetics, grooming.",
            BuiltInCategoryIds.FeesAndCharges => "Interest charges, bank fees, card fees, service charges.",
            BuiltInCategoryIds.CreditCardPayment => "Credit-card payments, statement payments, Capital One mobile payments.",
            BuiltInCategoryIds.Other => "Only for transactions that cannot reasonably fit another provided category.",
            _ => string.Empty
        };
    }
}
