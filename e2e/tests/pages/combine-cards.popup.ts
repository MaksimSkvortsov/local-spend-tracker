import { Page, Locator, expect } from '@playwright/test';
import { CardRow } from '../test-data/card-rows';

export interface CombineCardsPopupAssertOptions {
  /** The dialog is not shown. */
  isClosed?: boolean;
  source?: CardRow;
  target?: CardRow;
  /** The summary line: the transactions of `from` move to `to`. */
  moves?: { from: CardRow; to: CardRow };
}

/** How the dialog's selects label a card. */
export function combineOptionLabel(card: CardRow): string {
  return `${card.name} (${card.transactionCount} transactions)`;
}

/** The "Combine Cards" dialog opened by the Combine action of a card row. */
export class CombineCardsPopup {
  private readonly dialog: Locator;
  private readonly sourceSelect: Locator;
  private readonly targetSelect: Locator;
  private readonly summary: Locator;
  private readonly cancelButton: Locator;
  private readonly combineButton: Locator;

  constructor(page: Page) {
    this.dialog = page.getByRole('dialog', { name: 'Combine Cards', exact: true });
    this.sourceSelect = this.dialog.getByLabel('Source Card');
    this.targetSelect = this.dialog.getByLabel('Target Card');
    this.summary = this.dialog.getByText(/will move to/);
    this.cancelButton = this.dialog.getByRole('button', { name: 'Cancel' });
    this.combineButton = this.dialog.getByRole('button', { name: 'Combine Cards' });
  }

  async chooseSource(card: CardRow) {
    await this.sourceSelect.selectOption({ label: combineOptionLabel(card) });
  }

  async chooseTarget(card: CardRow) {
    await this.targetSelect.selectOption({ label: combineOptionLabel(card) });
  }

  async clickCancel() {
    await this.cancelButton.click();
  }

  async clickCombineCards() {
    await this.combineButton.click();
  }

  async assert(options: CombineCardsPopupAssertOptions = {}) {
    if (options.isClosed === true) {
      await expect(this.dialog).toBeHidden();
      return;
    }

    await expect(this.dialog).toBeVisible();
    if (options.source !== undefined) {
      await expect(this.sourceSelect.getByRole('option', { selected: true })).toHaveText(
        combineOptionLabel(options.source),
      );
    }
    if (options.target !== undefined) {
      await expect(this.targetSelect.getByRole('option', { selected: true })).toHaveText(
        combineOptionLabel(options.target),
      );
    }
    if (options.moves !== undefined) {
      const { from, to } = options.moves;
      await expect(this.summary).toHaveText(
        `${from.transactionCount} transactions from ${from.name} will move to ${to.name}. Transactions are not deduplicated.`,
      );
    }
  }
}
