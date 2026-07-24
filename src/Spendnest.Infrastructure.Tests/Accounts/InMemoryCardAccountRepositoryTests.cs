namespace Spendnest.Infrastructure.Tests.Accounts;

using FluentAssertions;
using Spendnest.Infrastructure.Accounts;

public class InMemoryCardAccountRepositoryTests
{
    [Fact]
    public async Task GetByNameAsync_ShouldReturnExistingCardAccountByName()
    {
        var repository = new InMemoryCardAccountRepository();

        var createdAccount = await repository.CreateAsync("Family Visa", CancellationToken.None);
        var foundAccount = await repository.GetByNameAsync(" family visa ", CancellationToken.None);

        foundAccount.Should().NotBeNull();
        foundAccount!.Id.Should().Be(createdAccount.Id);
        foundAccount.Name.Should().Be("Family Visa");
        var accounts = await repository.ListAsync(CancellationToken.None);
        accounts.Should().ContainSingle();
    }

    [Fact]
    public async Task GetByNameAsync_ShouldReturnNullWhenCardAccountDoesNotExist()
    {
        var repository = new InMemoryCardAccountRepository();

        var account = await repository.GetByNameAsync("Missing Card", CancellationToken.None);

        account.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldUseDefaultCardForBlankName()
    {
        var repository = new InMemoryCardAccountRepository();

        var account = await repository.CreateAsync(" ", CancellationToken.None);

        account.Name.Should().Be("Default Card");
    }
}
