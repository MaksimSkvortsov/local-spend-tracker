import { Page, Locator, expect } from '@playwright/test';
import { CardRow } from '../test-data/card-rows';

export interface CardsPageAssertOptions {
  isEmpty?: boolean;
  /** The only card listed, checked against the summary chip and its table row. */
  singleCard?: CardRow;
}

export class CardsPage {
  static readonly URL = '/cards';

  private readonly heading: Locator;
  private readonly cardCount: Locator;
  private readonly transactionCount: Locator;
  private readonly emptyState: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { name: 'Cards', level: 1 });
    // TODO: add data-testid for the cards summary chip counts
    this.cardCount = page.getByText(/^\d+ cards?$/);
    this.transactionCount = page.getByText(/^\d+ transactions?$/);
    this.emptyState = page.getByText('No cards imported yet.');
  }

  async goto() {
    await this.page.goto(CardsPage.URL);
  }

  async assert(options: CardsPageAssertOptions = {}) {
    await expect(this.page).toHaveURL(new RegExp(`${CardsPage.URL}$`));
    await expect(this.heading).toBeVisible();

    if (options.isEmpty === true) {
      await expect(this.cardCount).toHaveText('0 cards');
      await expect(this.transactionCount).toHaveText('0 transactions');
      await expect(this.emptyState).toBeVisible();
    }
    if (options.singleCard !== undefined) {
      const card = options.singleCard;
      await expect(this.cardCount).toHaveText('1 card');
      await expect(this.transactionCount).toHaveText(
        `${card.transactionCount} transaction${card.transactionCount === 1 ? '' : 's'}`,
      );
      const row = this.page.getByRole('row').filter({ hasText: card.name });
      await expect(row).toHaveCount(1);
      const cells = row.getByRole('cell');
      await expect(cells.nth(1)).toHaveText(String(card.transactionCount));
      await expect(cells.nth(2)).toHaveText(String(card.importCount));
      if (card.lastImported !== undefined) {
        await expect(cells.nth(3)).toHaveText(card.lastImported);
      }
      await expect(cells.nth(4)).toHaveText(card.status);
    }
  }
}
