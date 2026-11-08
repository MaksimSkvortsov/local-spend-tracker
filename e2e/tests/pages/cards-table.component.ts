import { Page, Locator, expect } from '@playwright/test';
import { CardRow } from '../test-data/card-rows';

export interface CardsTableAssertOptions {
  /** The rows in order, with the matching "Showing <n> cards" count. The name of a row being renamed is skipped, because it is shown in a textbox. */
  cards?: CardRow[];
  /** One row is being renamed: its textbox holds this value, with Save and Cancel in place of its other actions. */
  editing?: { value: string };
  /** The "No cards found." message is shown, or hidden when false. */
  noCardsFound?: boolean;
  /** The search box has focus. */
  searchFocused?: boolean;
  /** No row is being renamed, and every row shows Rename, Combine, and Delete. */
  idle?: boolean;
}

/** The table on the Cards page, with its row actions. */
export class CardsTableComponent {
  private readonly searchInput: Locator;
  private readonly shownCount: Locator;
  private readonly noCardsMessage: Locator;
  private readonly rows: Locator;
  private readonly renameInput: Locator;
  private readonly renameButtons: Locator;
  private readonly saveButton: Locator;
  private readonly cancelButton: Locator;

  constructor(private readonly page: Page) {
    this.searchInput = page.getByPlaceholder('Search cards by name...');
    // TODO: add data-testid for the shown-cards count
    this.shownCount = page.getByText(/^Showing \d+ cards?$/);
    this.noCardsMessage = page.getByText('No cards found.');
    this.rows = page.getByRole('row').filter({ has: page.getByRole('cell') });
    // TODO: add data-testid for the rename textbox
    this.renameInput = this.rows.getByRole('textbox');
    this.renameButtons = this.rows.getByRole('button', { name: 'Rename' });
    this.saveButton = this.rows.getByRole('button', { name: 'Save' });
    this.cancelButton = this.rows.getByRole('button', { name: 'Cancel' });
  }

  async search(text: string) {
    await this.searchInput.fill(text);
  }

  async clickRename(cardName: string) {
    await this.rowFor(cardName).getByRole('button', { name: 'Rename' }).click();
  }

  async fillRename(name: string) {
    await this.renameInput.fill(name);
  }

  async clickSave() {
    await this.saveButton.click();
  }

  async clickCancel() {
    await this.cancelButton.click();
  }

  async assert(options: CardsTableAssertOptions = {}) {
    if (options.cards !== undefined) {
      await expect(this.rows).toHaveCount(options.cards.length);
      await expect(this.shownCount).toHaveText(
        `Showing ${options.cards.length} card${options.cards.length === 1 ? '' : 's'}`,
      );
      for (const [index, card] of options.cards.entries()) {
        const row = this.rows.nth(index);
        const cells = row.getByRole('cell');
        if ((await row.getByRole('textbox').count()) === 0) {
          await expect(cells.nth(0).getByText(card.name, { exact: true })).toBeVisible();
        }
        await expect(cells.nth(1)).toHaveText(String(card.transactionCount));
        await expect(cells.nth(2)).toHaveText(String(card.importCount));
        if (card.lastImported !== undefined) {
          await expect(cells.nth(3)).toHaveText(card.lastImported);
        }
        await expect(cells.nth(4)).toHaveText(card.status);
      }
    }
    if (options.noCardsFound !== undefined) {
      await expect(this.noCardsMessage).toBeVisible({ visible: options.noCardsFound });
    }
    if (options.searchFocused === true) {
      await expect(this.searchInput).toBeFocused();
    }
    if (options.editing !== undefined) {
      await expect(this.renameInput).toHaveCount(1);
      await expect(this.renameInput).toHaveValue(options.editing.value);
      await expect(this.saveButton).toBeVisible();
      await expect(this.cancelButton).toBeVisible();
      await expect(this.renameButtons).toHaveCount((await this.rows.count()) - 1);
    }
    if (options.idle === true) {
      const rowCount = await this.rows.count();
      await expect(this.renameInput).toHaveCount(0);
      for (const action of ['Rename', 'Combine', 'Delete']) {
        await expect(this.rows.getByRole('button', { name: action })).toHaveCount(rowCount);
      }
    }
  }

  private rowFor(cardName: string): Locator {
    return this.rows.filter({ has: this.page.getByText(cardName, { exact: true }) });
  }
}
