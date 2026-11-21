import { Page, Locator, expect } from '@playwright/test';
import { TransactionRow } from '../test-data/transaction-rows';

export interface TransactionsTableAssertOptions {
  /** The listed rows in order. */
  rows?: TransactionRow[];
  /** The first listed row, when the rest are not checked. */
  firstRow?: TransactionRow;
  /** The footer reads "Showing <shown> of <total> transactions", or is not shown when null. */
  footer?: { shown: number; total: number } | null;
  previousPageEnabled?: boolean;
  nextPageEnabled?: boolean;
  /** The "No transactions match these filters." message is shown. */
  noMatches?: boolean;
  /** Card names that some listed row shows, and card names that no listed row shows. */
  cardsInRows?: { shown: string[]; hidden: string[] };
}

/** The transactions table with its paging footer (TransactionsTable). */
export class TransactionsTableComponent {
  private readonly rows: Locator;
  private readonly footerSummary: Locator;
  private readonly previousButton: Locator;
  private readonly nextButton: Locator;
  private readonly noMatchesMessage: Locator;

  constructor(private readonly page: Page) {
    this.rows = page.getByRole('row').filter({ has: page.getByRole('cell') });
    // TODO: add data-testid for the table footer summary
    this.footerSummary = page.getByText(/^Showing \d+ of \d+ transactions$/);
    this.previousButton = page.getByRole('button', { name: 'Previous page' });
    this.nextButton = page.getByRole('button', { name: 'Next page' });
    this.noMatchesMessage = page.getByText('No transactions match these filters.');
  }

  async clickNextPage() {
    await this.nextButton.click();
  }

  async clickPreviousPage() {
    await this.previousButton.click();
  }

  async clickConfirm(transaction: TransactionRow) {
    await this.rowFor(transaction).getByRole('button', { name: 'Confirm' }).click();
  }

  async chooseCategory(transaction: TransactionRow, category: string) {
    await this.rowFor(transaction).getByRole('combobox').selectOption({ label: category });
  }

  async assert(options: TransactionsTableAssertOptions = {}) {
    if (options.rows !== undefined) {
      await expect(this.rows).toHaveCount(options.rows.length);
      for (const [index, transaction] of options.rows.entries()) {
        await this.assertRow(this.rows.nth(index), transaction);
      }
    }
    if (options.firstRow !== undefined) {
      await this.assertRow(this.rows.first(), options.firstRow);
    }
    if (options.footer === null) {
      await expect(this.footerSummary).toBeHidden();
    } else if (options.footer !== undefined) {
      await expect(this.footerSummary).toHaveText(
        `Showing ${options.footer.shown} of ${options.footer.total} transactions`,
      );
    }
    if (options.previousPageEnabled !== undefined) {
      await expect(this.previousButton).toBeEnabled({ enabled: options.previousPageEnabled });
    }
    if (options.nextPageEnabled !== undefined) {
      await expect(this.nextButton).toBeEnabled({ enabled: options.nextPageEnabled });
    }
    if (options.noMatches !== undefined) {
      await expect(this.noMatchesMessage).toBeVisible({ visible: options.noMatches });
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

  /** A row found by its date and description, which together are unique in the seeded data. */
  private rowFor(transaction: TransactionRow): Locator {
    return this.rows.filter({ hasText: transaction.date }).filter({ hasText: transaction.description });
  }

  private async assertRow(row: Locator, transaction: TransactionRow) {
    const cells = row.getByRole('cell');
    await expect(cells.nth(0)).toHaveText(transaction.date);
    await expect(cells.nth(1)).toContainText(transaction.description);
    await expect(cells.nth(2).getByRole('option', { selected: true })).toHaveText(transaction.category);
    await expect(cells.nth(3)).toHaveText(transaction.card);
    if (transaction.amount !== undefined) {
      await expect(cells.nth(4)).toHaveText(transaction.amount);
    }
    await expect(row.getByText('Low Confidence')).toBeVisible({ visible: transaction.needsReview });
    await expect(row.getByRole('button', { name: 'Confirm' })).toBeVisible({ visible: transaction.needsReview });
  }
}
