import { Page, Locator, expect } from '@playwright/test';
import { ImportHistoryRow } from '../test-data/import-history';

export interface ImportHistoryComponentAssertOptions {
  isEmpty?: boolean;
  row?: ImportHistoryRow;
}

/** The Import History card of Import (ImportHistory). */
export class ImportHistoryComponent {
  private readonly heading: Locator;
  private readonly emptyState: Locator;
  private readonly table: Locator;

  constructor(page: Page) {
    this.heading = page.getByText('Import History', { exact: true });
    this.emptyState = page.getByText('No uploads in this session.');
    this.table = page.getByRole('table').filter({ has: page.getByRole('columnheader', { name: 'File Name' }) });
  }

  async assert(options: ImportHistoryComponentAssertOptions = {}) {
    await expect(this.heading).toBeVisible();

    if (options.isEmpty === true) {
      await expect(this.emptyState).toBeVisible();
    }
    if (options.row !== undefined) {
      const expected = options.row;
      const row = this.table.getByRole('row').filter({ hasText: expected.fileName });
      await expect(row).toHaveCount(1);
      const cells = row.getByRole('cell');
      await expect(cells.nth(1)).toHaveText(String(expected.count));
      await expect(cells.nth(2)).toHaveText(expected.transactionRange);
      await expect(cells.nth(3)).toHaveText(new RegExp(`^${expected.status}$`, 'i'));
    }
  }
}
