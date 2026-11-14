import { Cards } from './cards';
import { Statements } from './statements';

/** One row of the Cards table, as the Cards page shows it. */
export interface CardRow {
  name: string;
  transactionCount: number;
  importCount: number;
  /** Local date of the latest import; set only where the import time is seeded, not taken from the clock. */
  lastImported?: string;
  status: 'Active' | 'Empty' | 'Duplicate suspected';
}

export class CardRows {
  /** The catalog cards as Seeds.ThreeCards lists them, in table order. */
  static readonly BofaCheckingSeeded: CardRow = {
    name: Cards.BofaChecking.name,
    transactionCount: 22,
    importCount: 2,
    lastImported: 'Feb 1, 2026',
    status: 'Active',
  };

  static readonly CapOneDebitSeeded: CardRow = {
    name: Cards.CapOneDebit.name,
    transactionCount: 20,
    importCount: 2,
    lastImported: 'Feb 1, 2026',
    status: 'Active',
  };

  /** Seeded with ten transactions a month; the January import finishes Feb 1 in the harness time zone. */
  static readonly ChaseSapphireSeeded: CardRow = {
    name: Cards.ChaseSapphire.name,
    transactionCount: 20,
    importCount: 2,
    lastImported: 'Feb 1, 2026',
    status: 'Active',
  };

  static readonly EverydayDebitSeeded: CardRow = {
    ...CardRows.CapOneDebitSeeded,
    name: 'Everyday Debit',
  };

  /** Chase Sapphire and the renamed CapOne Debit flag each other as possible duplicates. */
  static readonly ChaseSapphireDuplicateSuspected: CardRow = {
    ...CardRows.ChaseSapphireSeeded,
    status: 'Duplicate suspected',
  };

  static readonly ChaseSapphireBackupDuplicateSuspected: CardRow = {
    ...CardRows.CapOneDebitSeeded,
    name: 'Chase Sapphire Backup',
    status: 'Duplicate suspected',
  };

  /** Chase Sapphire after the Backup card is combined into it. */
  static readonly ChaseSapphireCombined: CardRow = {
    ...CardRows.ChaseSapphireSeeded,
    transactionCount: 40,
    importCount: 4,
  };

  static readonly SapphirePreferredSeeded: CardRow = {
    ...CardRows.ChaseSapphireSeeded,
    name: 'Sapphire Preferred',
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
