/** Error messages the Transactions page shows above the table. The transaction id in them changes, so they are patterns. */
export class TransactionMessages {
  static readonly CategoryChangeNotFound = /^Could not update category: .* was not found\.$/;

  static readonly ConfirmNotFound = /^Could not confirm category: .* was not found\.$/;
}
