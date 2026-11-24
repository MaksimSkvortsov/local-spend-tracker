import { Page, Locator, expect } from '@playwright/test';
import { DashboardPeriod } from '../test-data/dashboard-periods';

export interface DashboardPeriodComponentAssertOptions {
  /** All mode shows no selects, Year mode shows the Year select, and Month mode shows both. */
  selects?: 'none' | 'year' | 'yearAndMonth';
  yearOptions?: string[];
  monthOptions?: string[];
  /** The year and month selected in the selects. */
  selected?: DashboardPeriod;
}

/** The report mode buttons and the Year and Month selects above the reports (DashboardPeriodSelector). */
export class DashboardPeriodComponent {
  private readonly allButton: Locator;
  private readonly yearButton: Locator;
  private readonly monthButton: Locator;
  private readonly yearSelect: Locator;
  private readonly monthSelect: Locator;

  constructor(page: Page) {
    const reportMode = page.getByRole('group', { name: 'Report mode' });
    this.allButton = reportMode.getByRole('button', { name: 'All', exact: true });
    this.yearButton = reportMode.getByRole('button', { name: 'Year', exact: true });
    this.monthButton = reportMode.getByRole('button', { name: 'Month', exact: true });
    this.yearSelect = page.getByLabel('Year', { exact: true });
    this.monthSelect = page.getByLabel('Month', { exact: true });
  }

  /** Reports on every transaction, instead of the window the dashboard opens on. */
  async clickAll() {
    await this.allButton.click();
  }

  async clickYear() {
    await this.yearButton.click();
  }

  async clickMonth() {
    await this.monthButton.click();
  }

  async chooseYear(year: string) {
    await this.yearSelect.selectOption({ label: year });
  }

  async chooseMonth(month: string) {
    await this.monthSelect.selectOption({ label: month });
  }

  async assert(options: DashboardPeriodComponentAssertOptions = {}) {
    if (options.selects !== undefined) {
      await expect(this.yearSelect).toBeVisible({ visible: options.selects !== 'none' });
      await expect(this.monthSelect).toBeVisible({ visible: options.selects === 'yearAndMonth' });
    }
    if (options.yearOptions !== undefined) {
      await expect(this.yearSelect.getByRole('option')).toHaveText(options.yearOptions);
    }
    if (options.monthOptions !== undefined) {
      await expect(this.monthSelect.getByRole('option')).toHaveText(options.monthOptions);
    }
    if (options.selected !== undefined) {
      await expect(this.yearSelect.getByRole('option', { selected: true })).toHaveText(options.selected.year);
      await expect(this.monthSelect.getByRole('option', { selected: true })).toHaveText(options.selected.month);
    }
  }
}
