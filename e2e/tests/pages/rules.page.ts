import { Page, Locator, expect } from '@playwright/test';

export interface RulesPageAssertOptions {
  isEmpty?: boolean;
}

export class RulesPage {
  static readonly URL = '/rules';

  private readonly heading: Locator;
  private readonly ruleCount: Locator;
  private readonly emptyState: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { name: 'Rules', level: 1 });
    this.ruleCount = page.getByText(/^\d+ rules?$/);
    this.emptyState = page.getByText('No category rules saved yet.');
  }

  async goto() {
    await this.page.goto(RulesPage.URL);
  }

  async assert(options: RulesPageAssertOptions = {}) {
    await expect(this.page).toHaveURL(new RegExp(`${RulesPage.URL}$`));
    await expect(this.heading).toBeVisible();

    if (options.isEmpty === true) {
      await expect(this.ruleCount).toHaveText('0 rules');
      await expect(this.emptyState).toBeVisible();
    }
  }
}
