import { Statements } from './statements';

/** One row of the Cards table, as the Cards page shows it. */
export interface CardRow {
  name: string;
  transactionCount: number;
  importCount: number;
  status: 'Active' | 'Empty';
}

export class CardRows {
  static readonly CapitalOneImported: CardRow = {
    name: Statements.CapitalOneDecember.detectedCardName,
    transactionCount: Statements.CapitalOneDecember.transactionCount,
    importCount: 1,
    status: 'Active',
  };

  /** A failed import keeps its card, with no transactions, so the import can be retried. */
  static readonly CapitalOneAfterFailedImport: CardRow = {
    name: Statements.CapitalOneDecember.detectedCardName,
    transactionCount: 0,
    importCount: 1,
    status: 'Empty',
  };
}
