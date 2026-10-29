import { Page, Locator, expect } from '@playwright/test';

export interface TransactionsPageAssertOptions {
  isEmpty?: boolean;
}

export class TransactionsPage {
  static readonly URL = '/transactions';

  private readonly heading: Locator;
  private readonly transactionCount: Locator;
  private readonly emptyState: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { name: 'Transactions', level: 1 });
    this.transactionCount = page.getByText(/^\d+ Transactions?$/);
    this.emptyState = page.getByText('No transactions imported yet.');
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
  }
}
