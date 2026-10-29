import path from 'path';

/**
 * Statement CSVs the harness returns in place of the native file dialog. Files live in test-data/statements/.
 */
export interface Statement {
  fileName: string;
  absolutePath: string;
  detectedCardName: string;
  transactionCount: number;
}

export class Statements {
  /** Capital One card 2246: one payment and three purchases posted on December 31, 2025. */
  static readonly CapitalOneDecember: Statement = {
    fileName: 'capital-one.csv',
    absolutePath: path.resolve(__dirname, 'statements', 'capital-one.csv'),
    detectedCardName: 'Capital One 2246',
    transactionCount: 4,
  };
}
