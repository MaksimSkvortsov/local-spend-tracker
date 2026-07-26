using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Maps transactions to categories using deterministic description keywords.
/// </summary>
public sealed class KeywordTransactionCategoryMapper : ITransactionCategoryMapper
{
    private static readonly (int CategoryId, string[] Keywords)[] Rules =
    [
        (BuiltInCategoryIds.CreditCardPayment, ["PAYMENT", "PYMT", "CARD MOBILE PAYMENT"]),
        (BuiltInCategoryIds.Groceries, ["GROCERY", "MARKET", "MERCADO", "BULK MART", "WAREHOUSE CLUB"]),
        (BuiltInCategoryIds.RestaurantsAndCoffee, ["CAFE", "CAFFE", "BISTRO", "RESTAURANT", "RESTAURANTE", "DINING"]),
        (BuiltInCategoryIds.Subscriptions, ["PLAN", "SUBSCRIPTION", "DOORBELL"]),
        (BuiltInCategoryIds.Utilities, ["WATER", "ELECTRIC", "UTILITY"]),
        (BuiltInCategoryIds.Transportation, ["GAS", "FUEL", "SUNOCO", "TRAIN", "TICKET"]),
        (BuiltInCategoryIds.Shopping, ["STORE", "SHOP", "MERCHANDISE"])
    ];

    public int MapCategoryId(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var description = transaction.OriginalDescription.ToUpperInvariant();

        foreach (var (categoryId, keywords) in Rules)
        {
            if (keywords.Any(description.Contains))
            {
                return categoryId;
            }
        }

        return transaction.Amount < 0
            ? BuiltInCategoryIds.Refund
            : BuiltInCategoryIds.Other;
    }
}
