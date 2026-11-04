import { Page, Locator, expect } from '@playwright/test';
import { AiSettingsFormComponent } from './ai-settings-form.component';
import { AiConfigurationActionsComponent } from './ai-configuration-actions.component';

export enum AiConnectionStatus {
  Connected = 'Connected',
  NotConfigured = 'Not configured',
}

export interface AiConfigurationComponentAssertOptions {
  status?: AiConnectionStatus;
  connectionSucceeded?: boolean;
  configurationDeleted?: boolean;
}

/**
 * The AI Configuration card of Settings (SettingsAiConfiguration): connection status and result message.
 * SettingsAiConfiguration is one Razor component, so its form and action sections are split logically.
 */
export class AiConfigurationComponent {
  readonly form: AiSettingsFormComponent;
  readonly actions: AiConfigurationActionsComponent;

  private readonly heading: Locator;
  private readonly connectionSucceededMessage: Locator;
  private readonly configurationDeletedMessage: Locator;

  constructor(private readonly page: Page) {
    this.form = new AiSettingsFormComponent(page);
    this.actions = new AiConfigurationActionsComponent(page);
    this.heading = page.getByRole('heading', { name: 'AI Configuration', level: 2 });
    this.connectionSucceededMessage = page.getByText('OpenAI connection succeeded.');
    this.configurationDeletedMessage = page.getByText('AI connection details deleted.');
  }

  async assert(options: AiConfigurationComponentAssertOptions = {}) {
    await expect(this.heading).toBeVisible();

    if (options.status !== undefined) {
      // TODO: add data-testid for the AI connection status badge
      await expect(this.page.getByText(options.status, { exact: true })).toBeVisible();
    }
    if (options.connectionSucceeded === true) {
      await expect(this.connectionSucceededMessage).toBeVisible();
    }
    if (options.configurationDeleted === true) {
      await expect(this.configurationDeletedMessage).toBeVisible();
    }
  }
}
