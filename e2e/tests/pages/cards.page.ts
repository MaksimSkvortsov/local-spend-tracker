import { Page, Locator, expect } from '@playwright/test';
import { CardRow } from '../test-data/card-rows';
import { CardsTableComponent } from './cards-table.component';

export interface CardsPageAssertOptions {
  isEmpty?: boolean;
  /** The only card listed, checked against the summary chip and its table row. */
  singleCard?: CardRow;
  /** The cards listed in order, checked against the summary chip and the table rows. */
  cards?: CardRow[];
  /** A status message that is shown. */
  message?: string;
  /** A status message that is not shown. */
  absentMessage?: string;
}

export class CardsPage {
  static readonly URL = '/cards';

  readonly table: CardsTableComponent;

  private readonly heading: Locator;
  private readonly cardCount: Locator;
  private readonly transactionCount: Locator;
  private readonly emptyState: Locator;

  constructor(private readonly page: Page) {
    this.table = new CardsTableComponent(page);
    this.heading = page.getByRole('heading', { name: 'Cards', level: 1 });
    // TODO: add data-testid for the cards summary chip counts
    this.cardCount = page.getByText(/^\d+ cards?$/);
    this.transactionCount = page.getByText(/^\d+ transactions?$/);
    this.emptyState = page.getByText('No cards imported yet.');
  }

  async goto() {
    await this.page.goto(CardsPage.URL);
  }

  async reload() {
    await this.page.reload();
  }

  async assert(options: CardsPageAssertOptions = {}) {
    await expect(this.page).toHaveURL(new RegExp(`${CardsPage.URL}$`));
    await expect(this.heading).toBeVisible();

    if (options.isEmpty === true) {
      await expect(this.cardCount).toHaveText('0 cards');
      await expect(this.transactionCount).toHaveText('0 transactions');
      await expect(this.emptyState).toBeVisible();
    }
    const cards = options.cards ?? (options.singleCard !== undefined ? [options.singleCard] : undefined);
    if (cards !== undefined) {
      const total = cards.reduce((sum, card) => sum + card.transactionCount, 0);
      await expect(this.cardCount).toHaveText(`${cards.length} card${cards.length === 1 ? '' : 's'}`);
      await expect(this.transactionCount).toHaveText(`${total} transaction${total === 1 ? '' : 's'}`);
      await this.table.assert({ cards });
    }
    if (options.message !== undefined) {
      await expect(this.page.getByText(options.message, { exact: true })).toBeVisible();
    }
    if (options.absentMessage !== undefined) {
      await expect(this.page.getByText(options.absentMessage, { exact: true })).toBeHidden();
    }
  }
}
