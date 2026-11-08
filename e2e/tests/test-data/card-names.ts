/** Names typed into the card rename box. */
export interface CardNameEntry {
  /** As typed, including surrounding spaces. */
  entered: string;
  /** As saved, trimmed. */
  saved: string;
}

export class CardNames {
  static readonly Draft: CardNameEntry = { entered: 'Draft', saved: 'Draft' };

  static readonly OnlySpaces: CardNameEntry = { entered: '   ', saved: '' };

  /** The name of CapOne Debit in a different case. */
  static readonly SameAsCapOneDebit: CardNameEntry = { entered: 'capone debit', saved: 'capone debit' };

  static readonly SapphirePreferred: CardNameEntry = {
    entered: '  Sapphire Preferred  ',
    saved: 'Sapphire Preferred',
  };
}
