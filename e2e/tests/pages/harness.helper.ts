import { APIRequestContext } from '@playwright/test';
import { Seed } from '../test-data/seeds';

export interface SeededImport {
  id: string;
  fileName: string;
  transactionCount: number;
  needsReviewCount: number;
  /** Amounts in spending categories: refunds reduce it, card payments and income are left out. */
  spendingTotal: number;
}

export interface SeededCard {
  id: string;
  name: string;
  imports: SeededImport[];
}

export interface SeedResult {
  settingsConfigured: boolean;
  cards: SeededCard[];
}

/**
 * Calls the UI harness control endpoints. Seeding first resets data, AI settings, the dashboard period,
 * and the selected statement file, so every test starts from the state it asks for.
 */
export class HarnessHelper {
  constructor(private readonly request: APIRequestContext) {}

  async seed(seed: Seed): Promise<SeedResult> {
    const response = await this.request.post('/harness/seed', {
      data: { settings: seed.settings, cards: seed.cards.map(card => card.key) },
    });
    if (!response.ok()) {
      throw new Error(`Seeding failed with ${response.status()}: ${await response.text()}`);
    }

    return (await response.json()) as SeedResult;
  }

  /** Sets the CSV the harness returns in place of the native file dialog. */
  async selectStatementFile(absolutePath: string): Promise<void> {
    const response = await this.request.put('/harness/statement-file', { data: { path: absolutePath } });
    if (!response.ok()) {
      throw new Error(`Selecting statement file failed with ${response.status()}: ${await response.text()}`);
    }
  }
}
