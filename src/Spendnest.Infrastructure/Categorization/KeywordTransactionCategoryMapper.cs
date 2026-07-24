using Spendnest.Core.Categories;
using Spendnest.Core.Categorization;
using Spendnest.Core.Transactions;

namespace Spendnest.Infrastructure.Categorization;

/// <summary>
/// Maps transactions to categories using deterministic description keywords.
/// </summary>
public sealed class KeywordTransactionCategoryMapper : ITransactionCategoryMapper
{
    private static readonly (string CategoryCode, string[] Keywords)[] Rules =
    [
        (BuiltInCategoryCodes.CreditCardPayment, ["PAYMENT", "PYMT", "CARD MOBILE PAYMENT"]),
        (BuiltInCategoryCodes.Groceries, ["GROCERY", "MARKET", "MERCADO", "BULK MART", "WAREHOUSE CLUB"]),
        (BuiltInCategoryCodes.RestaurantsAndCoffee, ["CAFE", "CAFFE", "BISTRO", "RESTAURANT", "RESTAURANTE", "DINING"]),
        (BuiltInCategoryCodes.Subscriptions, ["PLAN", "SUBSCRIPTION", "DOORBELL"]),
        (BuiltInCategoryCodes.Utilities, ["WATER", "ELECTRIC", "UTILITY"]),
        (BuiltInCategoryCodes.Transportation, ["GAS", "FUEL", "SUNOCO", "TRAIN", "TICKET"]),
        (BuiltInCategoryCodes.Shopping, ["STORE", "SHOP", "MERCHANDISE"])
    ];

    public string MapCategoryCode(Transaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);

        var description = transaction.OriginalDescription.ToUpperInvariant();

        foreach (var (categoryCode, keywords) in Rules)
        {
            if (keywords.Any(description.Contains))
            {
                return categoryCode;
            }
        }

        return transaction.Amount < 0
            ? BuiltInCategoryCodes.Refund
            : BuiltInCategoryCodes.Other;
    }
}
