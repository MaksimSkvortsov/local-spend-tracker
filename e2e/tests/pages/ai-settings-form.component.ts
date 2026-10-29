import { Page, Locator, expect } from '@playwright/test';
import { AiSettings } from '../test-data/ai-settings';

export interface AiSettingsFormComponentAssertOptions {
  formData?: Partial<AiSettings>;
  /** A saved key changes the key placeholder and help text to explain that a blank field keeps it. */
  hasSavedKey?: boolean;
}

/** The provider, OpenAI key, model, and timeout fields of the AI Configuration card. */
export class AiSettingsFormComponent {
  private readonly provider: Locator;
  private readonly apiKeyField: Locator;
  private readonly modelField: Locator;
  private readonly timeoutField: Locator;
  private readonly newKeyHelp: Locator;
  private readonly savedKeyHelp: Locator;

  constructor(page: Page) {
    this.provider = page.getByLabel('AI provider');
    this.apiKeyField = page.getByRole('textbox', { name: 'OpenAI API Key' });
    this.modelField = page.getByRole('combobox', { name: 'Model' });
    this.timeoutField = page.getByRole('spinbutton', { name: 'Request Timeout' });
    this.newKeyHelp = page.getByText('The key is stored in platform secure storage on this device.', { exact: false });
    this.savedKeyHelp = page.getByText('Leave blank to keep the saved key, or enter a new one to replace it.', {
      exact: false,
    });
  }

  async fillApiKey(apiKey: string) {
    await this.apiKeyField.fill(apiKey);
  }

  async getFormData(): Promise<Partial<AiSettings>> {
    return {
      apiKey: await this.apiKeyField.inputValue(),
      model: await this.modelField.inputValue(),
      timeoutSeconds: await this.timeoutField.inputValue(),
    };
  }

  async assert(options: AiSettingsFormComponentAssertOptions = {}) {
    await expect(this.provider).toHaveText(/ChatGPT/);

    if (options.formData !== undefined) {
      expect(await this.getFormData()).toMatchObject(options.formData);
    }
    if (options.hasSavedKey !== undefined) {
      const placeholder = options.hasSavedKey ? 'Saved key is configured' : 'sk-...';
      await expect(this.apiKeyField).toHaveAttribute('placeholder', placeholder);
      await expect(options.hasSavedKey ? this.savedKeyHelp : this.newKeyHelp).toBeVisible();
    }
  }
}
