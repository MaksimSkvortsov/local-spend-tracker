namespace Spendnest.Infrastructure.Tests.Categorization;

using FluentAssertions;
using Spendnest.Core.Transactions;
using Spendnest.Infrastructure.Categorization;

public class TransactionMerchantCodeResolverTests
{
    private readonly TransactionMerchantCodeResolver resolver = new();

    [Theory]
    [InlineData("TRADER JOE S #646", "TRADER JOE")]
    [InlineData("UBER   *TRIP", "UBER")]
    [InlineData("COSTCO WHSE #0218", "COSTCO WHSE")]
    [InlineData("TINY CINEMA #7781", "TINY CINEMA")]
    public void Resolve_ShouldNormalizeStatementDescriptionToMerchantCode(
        string description,
        string expectedCode)
    {
        resolver.Resolve(new Transaction
        {
            OriginalDescription = description
        }).Should().Be(expectedCode);
    }
}
