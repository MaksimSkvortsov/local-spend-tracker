import { Page, Locator, expect } from '@playwright/test';
import { ImportHistoryRow } from '../test-data/import-history';
import { Statement } from '../test-data/statements';
import { ImportActionsPopup } from './import-actions.popup';

export interface ImportHistoryComponentAssertOptions {
  isEmpty?: boolean;
  row?: ImportHistoryRow;
  rows?: ImportHistoryRow[];
  /** Files that are not listed. */
  absentFileNames?: string[];
  /** The statement a row action reported as imported, which only Retry Import does. */
  retriedStatement?: Statement;
}

/** The Import History card of Import (ImportHistory). */
export class ImportHistoryComponent {
  readonly actions: ImportActionsPopup;

  private readonly heading: Locator;
  private readonly emptyState: Locator;
  private readonly table: Locator;

  constructor(private readonly page: Page) {
    this.actions = new ImportActionsPopup(page);
    this.heading = page.getByText('Import History', { exact: true });
    this.emptyState = page.getByText('No uploads in this session.');
    this.table = page.getByRole('table').filter({ has: page.getByRole('columnheader', { name: 'File Name' }) });
  }

  async openRowActions(fileName: string) {
    await this.page.getByRole('button', { name: `Actions for ${fileName}` }).click();
  }

  async assert(options: ImportHistoryComponentAssertOptions = {}) {
    await expect(this.heading).toBeVisible();

    if (options.isEmpty === true) {
      await expect(this.emptyState).toBeVisible();
    }
    if (options.row !== undefined) {
      await this.assertRow(options.row);
    }
    for (const row of options.rows ?? []) {
      await this.assertRow(row);
    }
    for (const fileName of options.absentFileNames ?? []) {
      await expect(this.table.getByRole('row').filter({ hasText: fileName })).toHaveCount(0);
    }
    if (options.retriedStatement !== undefined) {
      const statement = options.retriedStatement;
      await expect(
        this.page.getByText(`${statement.fileName} was imported with ${statement.transactionCount} transactions.`),
      ).toBeVisible();
    }
  }

  private async assertRow(expected: ImportHistoryRow) {
    const row = this.table.getByRole('row').filter({ hasText: expected.fileName });
    await expect(row).toHaveCount(1);
    const cells = row.getByRole('cell');
    await expect(cells.nth(1)).toHaveText(String(expected.count));
    await expect(cells.nth(2)).toHaveText(expected.transactionRange);
    await expect(cells.nth(3)).toHaveText(new RegExp(`^${expected.status}$`, 'i'));
    if (expected.importedDate !== undefined) {
      await expect(cells.nth(4)).toHaveText(expected.importedDate);
    }
  }
}
