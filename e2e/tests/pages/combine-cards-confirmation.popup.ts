import { Page, Locator, expect } from '@playwright/test';
import { CardRow } from '../test-data/card-rows';

export interface CombineCardsConfirmationPopupAssertOptions {
  /** The dialog is not shown. */
  isClosed?: boolean;
  /** The cards the dialog asks to combine. */
  combining?: { source: CardRow; target: CardRow };
}

/** The "Combine cards?" confirmation shown after "Combine Cards" in the combine dialog. */
export class CombineCardsConfirmationPopup {
  private readonly dialog: Locator;
  private readonly cancelButton: Locator;
  private readonly confirmButton: Locator;

  constructor(page: Page) {
    this.dialog = page.getByRole('dialog', { name: 'Combine cards?', exact: true });
    this.cancelButton = this.dialog.getByRole('button', { name: 'Cancel' });
    this.confirmButton = this.dialog.getByRole('button', { name: 'Combine Cards' });
  }

  async clickCancel() {
    await this.cancelButton.click();
  }

  async clickConfirm() {
    await this.confirmButton.click();
  }

  async assert(options: CombineCardsConfirmationPopupAssertOptions = {}) {
    if (options.isClosed === true) {
      await expect(this.dialog).toBeHidden();
      return;
    }

    await expect(this.dialog).toBeVisible();
    await expect(this.cancelButton).toBeVisible();
    await expect(this.confirmButton).toBeVisible();
    if (options.combining !== undefined) {
      const { source, target } = options.combining;
      await expect(this.dialog).toContainText(
        `Combine ${source.name} into ${target.name}? Transactions are not deduplicated.`,
      );
    }
  }
}
