namespace Spendnest.Core.Tests.Transactions;

using FluentAssertions;
using Spendnest.Core.Transactions;

public class TransactionFingerprintTests
{
    [Fact]
    public void Create_ShouldNormalizeDescriptionWhitespaceAndCase()
    {
        var first = TransactionFingerprint.Create(
            new DateOnly(2026, 7, 18),
            141.83m,
            "Bulk   Mart #0218 Riverton VA");
        var second = TransactionFingerprint.Create(
            new DateOnly(2026, 7, 18),
            141.83m,
            "  BULK MART #0218 RIVERTON VA  ");

        first.Should().Be(second);
    }
}
