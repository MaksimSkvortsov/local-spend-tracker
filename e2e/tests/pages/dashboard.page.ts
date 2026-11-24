import { Page, Locator, expect } from '@playwright/test';
import { DashboardEmptyStateComponent } from './dashboard-empty-state.component';
import { DashboardPeriodComponent } from './dashboard-period.component';
import { DashboardReportsComponent } from './dashboard-reports.component';

export interface DashboardPageAssertOptions {
  /** The fresh-install onboarding panel is shown instead of reports. */
  isEmpty?: boolean;
  /** The reporting dashboard that replaces onboarding once a statement is imported. */
  reports?: { transactionCount: number };
  /** Cards named in Transaction Frequency, and cards that are not. */
  cardsInFrequency?: { shown: string[]; hidden: string[] };
}

export class DashboardPage {
  static readonly URL = '/dashboard';
  static readonly ROOT_URL = '/';

  readonly emptyState: DashboardEmptyStateComponent;
  readonly period: DashboardPeriodComponent;
  readonly reportCards: DashboardReportsComponent;

  private readonly onboardingChecklist: Locator;

  constructor(private readonly page: Page) {
    this.emptyState = new DashboardEmptyStateComponent(page);
    this.period = new DashboardPeriodComponent(page);
    this.reportCards = new DashboardReportsComponent(page);
    this.onboardingChecklist = page.getByLabel('Getting started checklist');
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
      await this.reportCards.assert({ absent: true });
    }
    if (options.reports !== undefined) {
      await expect(this.onboardingChecklist).toBeHidden();
      await this.reportCards.assert({ transactionCount: options.reports.transactionCount });
    }
    if (options.cardsInFrequency !== undefined) {
      for (const name of options.cardsInFrequency.shown) {
        await expect(this.page.getByLabel(`${name} transaction frequency`)).toBeVisible();
      }
      for (const name of options.cardsInFrequency.hidden) {
        await expect(this.page.getByLabel(`${name} transaction frequency`)).toHaveCount(0);
      }
    }
  }
}
