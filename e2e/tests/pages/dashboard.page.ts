import { Page, Locator, expect } from '@playwright/test';
import { DashboardEmptyStateComponent } from './dashboard-empty-state.component';

export interface DashboardPageAssertOptions {
  /** The fresh-install onboarding panel is shown instead of reports. */
  isEmpty?: boolean;
  /** The reporting dashboard that replaces onboarding once a statement is imported. */
  reports?: { transactionCount: number };
}

export class DashboardPage {
  static readonly URL = '/dashboard';
  static readonly ROOT_URL = '/';

  readonly emptyState: DashboardEmptyStateComponent;

  private readonly onboardingChecklist: Locator;
  private readonly totalSpendLabel: Locator;
  private readonly transactionsMetric: Locator;
  private readonly topMerchantsHeading: Locator;
  private readonly needsAttentionHeading: Locator;

  constructor(private readonly page: Page) {
    this.emptyState = new DashboardEmptyStateComponent(page);
    this.onboardingChecklist = page.getByLabel('Getting started checklist');
    this.totalSpendLabel = page.getByText('Total Spend', { exact: true });
    // TODO: add data-testid for the Transactions metric value
    this.transactionsMetric = page.getByRole('main').getByText(/^Transactions\s*\d+$/);
    this.topMerchantsHeading = page.getByText('Top Merchants', { exact: true });
    this.needsAttentionHeading = page.getByText('Needs Attention', { exact: true });
  }

  async goto() {
    await this.page.goto(DashboardPage.URL);
  }

  /** Opens the app root, which redirects to the dashboard. */
  async gotoAppRoot() {
    await this.page.goto(DashboardPage.ROOT_URL);
  }

  async reload() {
    await this.page.reload();
  }

  async assert(options: DashboardPageAssertOptions = {}) {
    await expect(this.page).toHaveURL(new RegExp(`${DashboardPage.URL}$`));
    await expect(this.page).toHaveTitle('Dashboard - PeasantMoney');

    if (options.isEmpty === true) {
      await expect(this.onboardingChecklist).toBeVisible();
      await expect(this.totalSpendLabel).toBeHidden();
    }
    if (options.reports !== undefined) {
      await expect(this.onboardingChecklist).toBeHidden();
      await expect(this.totalSpendLabel).toBeVisible();
      await expect(this.transactionsMetric).toHaveText(
        new RegExp(`^Transactions\\s*${options.reports.transactionCount}$`),
      );
      await expect(this.topMerchantsHeading).toBeVisible();
      await expect(this.needsAttentionHeading).toBeVisible();
    }
  }
}
