import { Page, Locator, expect } from '@playwright/test';
import { RuleRow } from '../test-data/rule-rows';

export interface RulesPageAssertOptions {
  isEmpty?: boolean;
  /** A rule listed in Saved Rules. */
  rule?: RuleRow;
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
    if (options.rule !== undefined) {
      const { pattern, matchType, category, matchCount } = options.rule;
      // TODO: add data-testid for the saved rule list items
      const listItem = this.page
        .getByRole('button')
        .filter({ hasText: new RegExp(`^${pattern}\\s*${matchCount ?? '\\d+'}\\s*matches\\s*${matchType}\\s*${category}$`) });
      await expect(listItem).toHaveCount(1);
    }
  }
}
