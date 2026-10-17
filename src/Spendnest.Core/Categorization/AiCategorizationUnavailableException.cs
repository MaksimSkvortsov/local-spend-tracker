namespace Spendnest.Core.Categorization;

/// <summary>
/// Raised when the AI categorizer cannot categorize at all, with a message that is safe to show to the user,
/// such as an exceeded quota or a missing API key.
/// </summary>
public sealed class AiCategorizationUnavailableException : InvalidOperationException
{
    public AiCategorizationUnavailableException(string reason, Exception? innerException = null)
        : base(reason, innerException)
    {
    }
}
