import { Statements } from './statements';

/** One row of the Import History table, as the Import page shows it. */
export interface ImportHistoryRow {
  fileName: string;
  count: number;
  transactionRange: string;
  status: 'Completed' | 'Failed';
  /** Local "Date Imported"; set only where the import time is seeded, not taken from the clock. */
  importedDate?: string;
}

export class ImportHistoryRows {
  /** Seeded imports finish at 02:00 UTC on the 2nd, which is the 1st in the harness time zone. */
  static readonly ChaseSapphireDecemberSeeded: ImportHistoryRow = {
    fileName: 'sapphire-2025-12.csv',
    count: 10,
    transactionRange: 'Dec 2, 2025 - Dec 26, 2025',
    status: 'Completed',
    importedDate: 'Jan 1, 2026',
  };

  static readonly ChaseSapphireJanuarySeeded: ImportHistoryRow = {
    fileName: 'sapphire-2026-01.csv',
    count: 10,
    transactionRange: 'Jan 2, 2026 - Jan 26, 2026',
    status: 'Completed',
    importedDate: 'Feb 1, 2026',
  };

  static readonly CapOneDecemberSeeded: ImportHistoryRow = {
    fileName: 'capone-2025-12.csv',
    count: 10,
    transactionRange: 'Dec 1, 2025 - Dec 24, 2025',
    status: 'Completed',
    importedDate: 'Jan 1, 2026',
  };

  static readonly CapOneJanuarySeeded: ImportHistoryRow = {
    fileName: 'capone-2026-01.csv',
    count: 10,
    transactionRange: 'Jan 1, 2026 - Jan 24, 2026',
    status: 'Completed',
    importedDate: 'Feb 1, 2026',
  };

  static readonly CapitalOneDecemberCompleted: ImportHistoryRow = {
    fileName: Statements.CapitalOneDecember.fileName,
    count: Statements.CapitalOneDecember.transactionCount,
    transactionRange: 'Dec 31, 2025',
    status: 'Completed',
  };

  /** Categorization failed, so nothing was saved and the range is empty. */
  static readonly CapitalOneDecemberFailed: ImportHistoryRow = {
    fileName: Statements.CapitalOneDecember.fileName,
    count: 0,
    transactionRange: '--',
    status: 'Failed',
  };
}
