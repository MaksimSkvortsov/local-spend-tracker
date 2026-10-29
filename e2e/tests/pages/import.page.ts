import { Page, Locator, expect } from '@playwright/test';
import { ImportUploadPromptComponent } from './import-upload-prompt.component';
import { SelectedTransactionsPreviewComponent } from './selected-transactions-preview.component';
import { ImportHistoryComponent } from './import-history.component';

/** Import, composed of components that mirror the app's Import cards. */
export class ImportPage {
  static readonly URL = '/import';

  readonly uploadPrompt: ImportUploadPromptComponent;
  readonly preview: SelectedTransactionsPreviewComponent;
  readonly history: ImportHistoryComponent;

  private readonly heading: Locator;

  constructor(private readonly page: Page) {
    this.uploadPrompt = new ImportUploadPromptComponent(page);
    this.preview = new SelectedTransactionsPreviewComponent(page);
    this.history = new ImportHistoryComponent(page);
    this.heading = page.getByRole('heading', { name: 'Import Transactions', level: 1 });
  }

  async goto() {
    await this.page.goto(ImportPage.URL);
  }

  async assert() {
    await expect(this.page).toHaveURL(new RegExp(`${ImportPage.URL}$`));
    await expect(this.heading).toBeVisible();
  }
}
