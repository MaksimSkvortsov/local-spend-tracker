import { Page, Locator, expect } from '@playwright/test';

export interface ImportActionsPopupAssertOptions {
  /** The failed import the panel offers to retry or delete. */
  failedFileName?: string;
}

/** The "Retry or delete import" panel opened from an Import History row's actions button. */
export class ImportActionsPopup {
  private readonly panel: Locator;
  private readonly cancelButton: Locator;
  private readonly retryButton: Locator;
  private readonly deleteButton: Locator;

  constructor(page: Page) {
    this.panel = page.getByRole('group', { name: 'Retry or delete import' });
    this.cancelButton = this.panel.getByRole('button', { name: 'Cancel' });
    this.retryButton = this.panel.getByRole('button', { name: 'Retry Import' });
    this.deleteButton = this.panel.getByRole('button', { name: 'Delete Import' });
  }

  async clickRetryImport() {
    await this.retryButton.click();
  }

  async assert(options: ImportActionsPopupAssertOptions = {}) {
    await expect(this.panel).toBeVisible();
    await expect(this.cancelButton).toBeVisible();
    await expect(this.retryButton).toBeVisible();
    await expect(this.deleteButton).toBeVisible();

    if (options.failedFileName !== undefined) {
      await expect(this.panel).toContainText(
        `Retry importing "${options.failedFileName}", or delete this failed import. No transactions were saved from it.`,
      );
    }
  }
}
