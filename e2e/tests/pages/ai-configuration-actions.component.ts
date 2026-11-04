import { Page, Locator, expect } from '@playwright/test';

export interface AiConfigurationActionsComponentAssertOptions {
  deleteConfigurationEnabled?: boolean;
}

/** The Test Connection, Save, and Delete Configuration buttons of the AI Configuration card. */
export class AiConfigurationActionsComponent {
  private readonly testConnectionButton: Locator;
  private readonly saveButton: Locator;
  private readonly deleteConfigurationButton: Locator;

  constructor(page: Page) {
    this.testConnectionButton = page.getByRole('button', { name: 'Test Connection' });
    this.saveButton = page.getByRole('button', { name: 'Save', exact: true });
    this.deleteConfigurationButton = page.getByRole('button', { name: 'Delete Configuration' });
  }

  async clickTestConnection() {
    await this.testConnectionButton.click();
  }

  async clickSave() {
    await this.saveButton.click();
  }

  async clickDeleteConfiguration() {
    await this.deleteConfigurationButton.click();
  }

  async assert(options: AiConfigurationActionsComponentAssertOptions = {}) {
    await expect(this.testConnectionButton).toBeVisible();
    await expect(this.saveButton).toBeVisible();

    if (options.deleteConfigurationEnabled !== undefined) {
      if (options.deleteConfigurationEnabled) {
        await expect(this.deleteConfigurationButton).toBeEnabled();
      } else {
        await expect(this.deleteConfigurationButton).toBeDisabled();
      }
    }
  }
}
