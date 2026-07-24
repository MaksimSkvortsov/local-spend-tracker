namespace Spendnest.Infrastructure.Tests.Credentials;

using FluentAssertions;
using Spendnest.Core.Credentials;
using Spendnest.Infrastructure.Credentials;

public class InMemoryCredentialStoreTests
{
    [Fact]
    public async Task GetStringAsync_ShouldReturnInitialValueForKey()
    {
        var store = new InMemoryCredentialStore(new Dictionary<string, string?>
        {
            [CredentialKeys.OpenAiApiKey] = " openai-key "
        });

        var apiKey = await store.GetStringAsync(CredentialKeys.OpenAiApiKey, CancellationToken.None);

        apiKey.Should().Be("openai-key");
    }

    [Fact]
    public async Task SaveStringAsync_ShouldStoreSeparateValuesByKey()
    {
        var store = new InMemoryCredentialStore();

        await store.SaveStringAsync(CredentialKeys.OpenAiApiKey, " openai-key ", CancellationToken.None);
        await store.SaveStringAsync(CredentialKeys.GeminiApiKey, " gemini-key ", CancellationToken.None);

        var openAiKey = await store.GetStringAsync(CredentialKeys.OpenAiApiKey, CancellationToken.None);
        var geminiKey = await store.GetStringAsync(CredentialKeys.GeminiApiKey, CancellationToken.None);
        openAiKey.Should().Be("openai-key");
        geminiKey.Should().Be("gemini-key");
    }

    [Fact]
    public async Task ClearAsync_ShouldRemoveOnlySelectedKey()
    {
        var store = new InMemoryCredentialStore();
        await store.SaveStringAsync(CredentialKeys.OpenAiApiKey, "openai-key", CancellationToken.None);
        await store.SaveStringAsync(CredentialKeys.GeminiApiKey, "gemini-key", CancellationToken.None);

        await store.ClearAsync(CredentialKeys.OpenAiApiKey, CancellationToken.None);

        var openAiKey = await store.GetStringAsync(CredentialKeys.OpenAiApiKey, CancellationToken.None);
        var geminiKey = await store.GetStringAsync(CredentialKeys.GeminiApiKey, CancellationToken.None);
        openAiKey.Should().BeNull();
        geminiKey.Should().Be("gemini-key");
    }
}
