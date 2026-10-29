import { Page, Locator, expect } from '@playwright/test';
import { AiConfigurationComponent } from './ai-configuration.component';

/** Settings, composed of section components that mirror the app's Settings cards. */
export class SettingsPage {
  static readonly URL = '/settings';

  readonly aiConfiguration: AiConfigurationComponent;

  private readonly heading: Locator;

  constructor(private readonly page: Page) {
    this.aiConfiguration = new AiConfigurationComponent(page);
    this.heading = page.getByRole('heading', { name: 'Settings', level: 1 });
  }

  async goto() {
    await this.page.goto(SettingsPage.URL);
  }

  async assert() {
    await expect(this.page).toHaveURL(new RegExp(`${SettingsPage.URL}$`));
    await expect(this.heading).toBeVisible();
  }
}
