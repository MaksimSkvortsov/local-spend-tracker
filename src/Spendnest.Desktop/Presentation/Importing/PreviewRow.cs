namespace Spendnest.Desktop.Presentation.Importing;

public sealed record PreviewRow(
    DateOnly PostedDate,
    string Description,
    string Category,
    decimal Amount);
