import { Page, Locator, expect } from '@playwright/test';

export interface TransactionsPageAssertOptions {
  isEmpty?: boolean;
  transactionCount?: number;
  /** The options of the Card filter, in order. */
  cardFilterOptions?: string[];
  /** Card names that some listed row shows, and card names that no listed row shows. */
  cardsInRows?: { shown: string[]; hidden: string[] };
}

export class TransactionsPage {
  static readonly URL = '/transactions';

  private readonly heading: Locator;
  private readonly transactionCount: Locator;
  private readonly emptyState: Locator;
  private readonly cardFilter: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { name: 'Transactions', level: 1 });
    this.transactionCount = page.getByText(/^\d+ Transactions?$/);
    this.emptyState = page.getByText('No transactions imported yet.');
    this.cardFilter = page.getByLabel('Card', { exact: true });
  }

  async goto() {
    await this.page.goto(TransactionsPage.URL);
  }

  async assert(options: TransactionsPageAssertOptions = {}) {
    await expect(this.page).toHaveURL(new RegExp(`${TransactionsPage.URL}$`));
    await expect(this.heading).toBeVisible();

    if (options.isEmpty === true) {
      await expect(this.transactionCount).toHaveText('0 Transactions');
      await expect(this.emptyState).toBeVisible();
    }
    if (options.transactionCount !== undefined) {
      await expect(this.transactionCount).toHaveText(
        `${options.transactionCount} Transaction${options.transactionCount === 1 ? '' : 's'}`,
      );
    }
    if (options.cardFilterOptions !== undefined) {
      await expect(this.cardFilter.getByRole('option')).toHaveText(options.cardFilterOptions);
    }
    if (options.cardsInRows !== undefined) {
      for (const name of options.cardsInRows.shown) {
        await expect(this.page.getByRole('cell', { name, exact: true }).first()).toBeVisible();
      }
      for (const name of options.cardsInRows.hidden) {
        await expect(this.page.getByRole('cell', { name, exact: true })).toHaveCount(0);
      }
    }
  }
}
