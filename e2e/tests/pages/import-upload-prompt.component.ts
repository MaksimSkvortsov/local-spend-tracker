import { Page, Locator, expect } from '@playwright/test';

/** The Upload Statement card of Import (ImportUploadPrompt), shown when no statement is being previewed. */
export class ImportUploadPromptComponent {
  private readonly heading: Locator;
  private readonly browseFilesButton: Locator;

  constructor(page: Page) {
    this.heading = page.getByText('Upload Statement', { exact: true });
    this.browseFilesButton = page.getByRole('button', { name: /browse files from your computer/ });
  }

  async clickBrowseFiles() {
    await this.browseFilesButton.click();
  }

  async assert() {
    await expect(this.heading).toBeVisible();
    await expect(this.browseFilesButton).toBeVisible();
  }
}
