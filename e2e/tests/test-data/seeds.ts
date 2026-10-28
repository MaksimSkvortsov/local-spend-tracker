import { Card, Cards } from './cards';

/**
 * A starting state for a test. `settings` saves a fake OpenAI key with the default model and timeout;
 * `cards` adds catalog cards with their December 2025 and January 2026 transactions.
 */
export interface Seed {
  settings: boolean;
  cards: Card[];
}

export class Seeds {
  /** A fresh install: no AI settings and no data. */
  static readonly NewUser: Seed = { settings: false, cards: [] };

  /** AI configured and one card, for flows that add more. */
  static readonly OneCard: Seed = { settings: true, cards: [Cards.ChaseSapphire] };

  /** AI configured and every catalog card, for checking that seeded data shows everywhere. */
  static readonly ThreeCards: Seed = {
    settings: true,
    cards: [Cards.ChaseSapphire, Cards.CapOneDebit, Cards.BofaChecking],
  };
}
