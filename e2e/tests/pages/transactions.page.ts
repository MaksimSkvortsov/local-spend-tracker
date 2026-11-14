import { Page, Locator, expect } from '@playwright/test';
import { TransactionsToolbarComponent } from './transactions-toolbar.component';
import { TransactionsTableComponent } from './transactions-table.component';

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

  readonly toolbar: TransactionsToolbarComponent;
  readonly table: TransactionsTableComponent;

  private readonly heading: Locator;
  private readonly emptyState: Locator;

  constructor(private readonly page: Page) {
    this.toolbar = new TransactionsToolbarComponent(page);
    this.table = new TransactionsTableComponent(page);
    this.heading = page.getByRole('heading', { name: 'Transactions', level: 1 });
    this.emptyState = page.getByText('No transactions imported yet.');
  }

  async goto() {
    await this.page.goto(TransactionsPage.URL);
  }

  async reload() {
    await this.page.reload();
  }

  async assert(options: TransactionsPageAssertOptions = {}) {
    await expect(this.page).toHaveURL(new RegExp(`${TransactionsPage.URL}$`));
    await expect(this.heading).toBeVisible();

    if (options.isEmpty === true) {
      await this.toolbar.assert({ transactionCount: 0 });
      await expect(this.emptyState).toBeVisible();
    }
    if (options.transactionCount !== undefined || options.cardFilterOptions !== undefined) {
      await this.toolbar.assert({
        transactionCount: options.transactionCount,
        cardFilterOptions: options.cardFilterOptions,
      });
    }
    if (options.cardsInRows !== undefined) {
      await this.table.assert({ cardsInRows: options.cardsInRows });
    }
  }
}
