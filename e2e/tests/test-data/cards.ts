/**
 * Cards the harness can seed. Keys and names must match SeedCardCatalog in Spendnest.UiHarness.
 * Each card gets one import for December 2025 and one for January 2026.
 */
export interface Card {
  key: string;
  name: string;
}

export class Cards {
  static readonly ChaseSapphire: Card = { key: 'sapphire', name: 'Chase Sapphire' };

  static readonly CapOneDebit: Card = { key: 'capone', name: 'CapOne Debit' };

  static readonly BofaChecking: Card = { key: 'boa', name: 'BofA Checking' };
}
