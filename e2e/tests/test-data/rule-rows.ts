/** One rule in the Saved Rules list of the Rules page. */
export interface RuleRow {
  pattern: string;
  matchType: 'Exact' | 'Prefix' | 'Contains';
  category: string;
  /** The number of transactions the rule matches; not checked when left out. */
  matchCount?: number;
}

export class RuleRows {
  /** The rule every seeded card adds. */
  static readonly Uber: RuleRow = { pattern: 'UBER', matchType: 'Contains', category: 'Transportation' };

  /** The same rule once no transaction is left to match. */
  static readonly UberWithNoMatches: RuleRow = { ...RuleRows.Uber, matchCount: 0 };
}
