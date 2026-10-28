namespace Spendnest.UiHarness.Tests.Seeding;

using FluentAssertions;
using Spendnest.UiHarness.Seeding;

public class HarnessSeedPlanTests
{
    [Fact]
    public void FromRequest_WithEmptyRequest_ShouldSeedNewUser()
    {
        var plan = HarnessSeedPlan.FromRequest(new HarnessSeedRequest(null, null));

        plan.Settings.Should().BeFalse();
        plan.Cards.Should().BeEmpty();
    }

    [Fact]
    public void FromRequest_ShouldResolveCardKeysInRequestOrder()
    {
        var plan = HarnessSeedPlan.FromRequest(new HarnessSeedRequest(true, ["boa", "SAPPHIRE"]));

        plan.Settings.Should().BeTrue();
        plan.Cards.Should().Equal(SeedCardCatalog.BofaChecking, SeedCardCatalog.ChaseSapphire);
    }

    [Fact]
    public void FromRequest_WithUnknownCard_ShouldListKnownCards()
    {
        var act = () => HarnessSeedPlan.FromRequest(new HarnessSeedRequest(true, ["amex"]));

        act.Should().Throw<ArgumentException>()
            .WithMessage("Unknown card 'amex'. Known cards: sapphire, capone, boa.");
    }

    [Fact]
    public void FromRequest_WithRepeatedCard_ShouldReject()
    {
        var act = () => HarnessSeedPlan.FromRequest(new HarnessSeedRequest(true, ["capone", "CapOne"]));

        act.Should().Throw<ArgumentException>()
            .WithMessage("Cards are listed more than once: capone.");
    }
}
