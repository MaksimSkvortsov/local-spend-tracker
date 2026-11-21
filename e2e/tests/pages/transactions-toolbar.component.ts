import { Page, Locator, expect } from '@playwright/test';

export interface TransactionsToolbarAssertOptions {
  /** The "<n> Transactions" chip. */
  transactionCount?: number;
  /** The number on the Require Review button; none is shown for 0. */
  reviewCount?: number;
  /** The selected option of the Category filter. */
  category?: string;
  /** The selected option of the Card filter. */
  card?: string;
  /** The options of the Card filter, in order. */
  cardFilterOptions?: string[];
}

/** The review toggle, count chip, and filters above the Transactions table (TransactionsToolbar). */
export class TransactionsToolbarComponent {
  private readonly allButton: Locator;
  private readonly requireReviewButton: Locator;
  private readonly transactionCount: Locator;
  private readonly categoryFilter: Locator;
  private readonly cardFilter: Locator;

  constructor(page: Page) {
    const reviewFilter = page.getByRole('group', { name: 'Review filter' });
    this.allButton = reviewFilter.getByRole('button', { name: 'All', exact: true });
    this.requireReviewButton = reviewFilter.getByRole('button', { name: /^Require Review/ });
    this.transactionCount = page.getByText(/^\d+ Transactions?$/);
    this.categoryFilter = page.getByLabel('Category', { exact: true });
    this.cardFilter = page.getByLabel('Card', { exact: true });
  }

  async clickAll() {
    await this.allButton.click();
  }

  async clickRequireReview() {
    await this.requireReviewButton.click();
  }

  async chooseCategory(category: string) {
    await this.categoryFilter.selectOption({ label: category });
  }

  async chooseCard(card: string) {
    await this.cardFilter.selectOption({ label: card });
  }

  async assert(options: TransactionsToolbarAssertOptions = {}) {
    if (options.transactionCount !== undefined) {
      await expect(this.transactionCount).toHaveText(
        `${options.transactionCount} Transaction${options.transactionCount === 1 ? '' : 's'}`,
      );
    }
    if (options.reviewCount !== undefined) {
      await expect(this.requireReviewButton).toHaveText(
        new RegExp(`^\\s*Require Review\\s*${options.reviewCount === 0 ? '' : options.reviewCount}\\s*$`),
      );
    }
    if (options.category !== undefined) {
      await expect(this.categoryFilter.getByRole('option', { selected: true })).toHaveText(options.category);
    }
    if (options.card !== undefined) {
      await expect(this.cardFilter.getByRole('option', { selected: true })).toHaveText(options.card);
    }
    if (options.cardFilterOptions !== undefined) {
      await expect(this.cardFilter.getByRole('option')).toHaveText(options.cardFilterOptions);
    }
  }
}
