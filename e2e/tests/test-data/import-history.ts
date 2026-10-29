import { Statements } from './statements';

/** One row of the Import History table, as the Import page shows it. */
export interface ImportHistoryRow {
  fileName: string;
  count: number;
  transactionRange: string;
  status: 'Completed' | 'Failed';
}

export class ImportHistoryRows {
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
