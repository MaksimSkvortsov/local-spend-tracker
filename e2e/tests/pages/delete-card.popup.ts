import { Page, Locator, expect } from '@playwright/test';

export interface DeleteCardPopupAssertOptions {
  /** The dialog is not shown. */
  isClosed?: boolean;
  /** The card the dialog asks to delete. */
  cardName?: string;
}

/** The "Delete "<card>"?" confirmation opened by the Delete action of a card row. */
export class DeleteCardPopup {
  private readonly dialog: Locator;
  private readonly heading: Locator;
  private readonly cancelButton: Locator;
  private readonly deleteButton: Locator;

  constructor(page: Page) {
    this.dialog = page.getByRole('dialog', { name: /^Delete ".*"\?$/ });
    this.heading = this.dialog.getByRole('heading');
    this.cancelButton = this.dialog.getByRole('button', { name: 'Cancel' });
    this.deleteButton = this.dialog.getByRole('button', { name: 'Delete Card' });
  }

  async clickCancel() {
    await this.cancelButton.click();
  }

  async clickDeleteCard() {
    await this.deleteButton.click();
  }

  async assert(options: DeleteCardPopupAssertOptions = {}) {
    if (options.isClosed === true) {
      await expect(this.dialog).toBeHidden();
      return;
    }

    await expect(this.dialog).toBeVisible();
    await expect(this.dialog).toContainText(
      'Are you sure you want to delete this card and its associated transaction history?',
    );
    await expect(this.cancelButton).toBeVisible();
    await expect(this.deleteButton).toBeVisible();
    if (options.cardName !== undefined) {
      await expect(this.heading).toHaveText(`Delete "${options.cardName}"?`);
    }
  }
}
