import { Cards } from './cards';
import { Statements } from './statements';

/** One row of the Cards table, as the Cards page shows it. */
export interface CardRow {
  name: string;
  transactionCount: number;
  importCount: number;
  /** Local date of the latest import; set only where the import time is seeded, not taken from the clock. */
  lastImported?: string;
  status: 'Active' | 'Empty';
}

export class CardRows {
  /** Seeded with ten transactions a month; the January import finishes Feb 1 in the harness time zone. */
  static readonly ChaseSapphireSeeded: CardRow = {
    name: Cards.ChaseSapphire.name,
    transactionCount: 20,
    importCount: 2,
    lastImported: 'Feb 1, 2026',
    status: 'Active',
  };

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
