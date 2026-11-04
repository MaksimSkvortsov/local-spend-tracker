import { Page, Locator, expect } from '@playwright/test';
import { Statement } from '../test-data/statements';

export interface SelectedTransactionsPreviewComponentAssertOptions {
  /** The statement whose transactions are previewed before confirming. */
  statement?: Statement;
  /** The import failed because AI is not configured. */
  isMissingApiKeyError?: boolean;
}

/** The Transactions in File card of Import (SelectedTransactionsPreview), shown after a statement is picked. */
export class SelectedTransactionsPreviewComponent {
  private readonly heading: Locator;
  private readonly detectedCardText: Locator;
  private readonly cancelButton: Locator;
  private readonly confirmImportButton: Locator;
  private readonly missingApiKeyError: Locator;
  private readonly rows: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByText('Transactions in File', { exact: true });
    this.detectedCardText = page.getByText('Card detected from the selected CSV.');
    this.cancelButton = page.getByRole('button', { name: 'Cancel' });
    this.confirmImportButton = page.getByRole('button', { name: 'Confirm Import' });
    // The same message also appears in the Import History row, so take the preview's, which comes first.
    this.missingApiKeyError = page.getByText(/no transactions were saved.*API key is required/).first();
    this.rows = page
      .getByRole('table')
      .filter({ has: page.getByRole('columnheader', { name: 'Description' }) })
      .getByRole('row')
      .filter({ has: page.getByRole('cell') });
  }

  async clickCancel() {
    await this.cancelButton.click();
  }

  async clickConfirmImport() {
    await this.confirmImportButton.click();
  }

  async assert(options: SelectedTransactionsPreviewComponentAssertOptions = {}) {
    await expect(this.heading).toBeVisible();
    await expect(this.cancelButton).toBeVisible();

    if (options.statement !== undefined) {
      const statement = options.statement;
      await expect(
        this.page.getByText(`Previewing ${statement.transactionCount} transactions from ${statement.fileName}`),
      ).toBeVisible();
      await expect(this.page.getByText(statement.detectedCardName, { exact: true })).toBeVisible();
      await expect(this.detectedCardText).toBeVisible();
      await expect(this.rows).toHaveCount(statement.transactionCount);
      await expect(this.confirmImportButton).toBeEnabled();
    }
    if (options.isMissingApiKeyError === true) {
      await expect(this.missingApiKeyError).toBeVisible();
    }
  }
}
