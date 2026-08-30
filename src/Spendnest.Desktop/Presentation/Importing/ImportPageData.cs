using Spendnest.Core.Accounts;

namespace Spendnest.Desktop.Presentation.Importing;

public sealed record ImportPageData(
    IReadOnlyList<CardAccount> AvailableCards,
    IReadOnlyList<UploadHistoryItem> UploadHistory);
