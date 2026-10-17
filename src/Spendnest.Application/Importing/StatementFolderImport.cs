namespace Spendnest.Application.Importing;

/// <summary>
/// Requests importing a folder of statement files to the card named after the folder.
/// </summary>
public sealed record StatementFolderImport(
    string CardName,
    IReadOnlyList<string> FilePaths,
    bool CreateCard);
