import { Page, Locator, expect } from '@playwright/test';
import { DashboardReport } from '../test-data/dashboard-reports';

export interface DashboardReportsComponentAssertOptions {
  /** No report is shown, as on the onboarding dashboard. */
  absent?: boolean;
  /** Only the number of transactions in the window. */
  transactionCount?: number;
  /** Everything the dashboard reports for the window. */
  report?: DashboardReport;
}

const REPORT_CARDS = [
  'Spending by Category',
  'Top Merchants',
  'Needs Attention',
  'Biggest Transactions',
  'Transaction Frequency',
  'Amount Spent per Day',
];

/** The metric panel and report cards of the dashboard once transactions exist. */
export class DashboardReportsComponent {
  private readonly windowMetric: Locator;
  private readonly totalSpend: Locator;
  private readonly transactionsMetric: Locator;
  private readonly categoryTotal: Locator;
  private readonly biggestRows: Locator;
  private readonly reviewNowLink: Locator;

  constructor(private readonly page: Page) {
    // TODO: add data-testid for the metric values and the category chart total
    this.windowMetric = page.getByText(/^\s*Window\s*\S/);
    this.totalSpend = page.getByText(/^\s*Total Spend\s*\S/);
    this.transactionsMetric = page.getByRole('main').getByText(/^\s*Transactions\s*\d+\s*$/);
    this.categoryTotal = page.getByText(/^\s*Total\s*\$/);
    this.biggestRows = page.getByRole('row').filter({ has: page.getByRole('cell') });
    this.reviewNowLink = page.getByRole('link', { name: 'Review now' });
  }

  async assert(options: DashboardReportsComponentAssertOptions = {}) {
    if (options.absent === true) {
      await expect(this.totalSpend).toBeHidden();
      return;
    }

    if (options.transactionCount !== undefined) {
      await this.assertTransactionCount(options.transactionCount);
    }
    if (options.report !== undefined) {
      await this.assertReport(options.report);
    }
  }

  private async assertTransactionCount(count: number) {
    await expect(this.totalSpend).toBeVisible();
    await expect(this.transactionsMetric).toHaveText(new RegExp(`^Transactions\\s*${count}$`));
    for (const title of REPORT_CARDS) {
      await expect(this.page.getByRole('heading', { name: title, exact: true })).toBeVisible();
    }
  }

  private async assertReport(report: DashboardReport) {
    await this.assertTransactionCount(report.transactionCount);
    await expect(this.windowMetric).toHaveText(new RegExp(`^Window\\s*${escape(report.window)}$`));
    await expect(this.totalSpend).toHaveText(new RegExp(`^Total Spend\\s*${escape(report.totalSpend)}$`));

    if (report.categories !== undefined) {
      const { total, largest } = report.categories;
      await expect(this.categoryTotal).toHaveText(new RegExp(`^Total\\s*${escape(total)}$`));
      await expect(
        this.page.getByText(new RegExp(`^\\s*${escape(largest.name)}\\s*${escape(largest.amount)}\\s*$`)),
      ).toBeVisible();
    } else {
      await expect(this.page.getByText('No category spending in this window.')).toBeVisible();
    }

    if (report.topMerchant !== undefined) {
      const { name, amount, transactionCount } = report.topMerchant;
      await expect(
        this.page.getByText(new RegExp(`^\\s*${escape(name)}\\s*${escape(amount)}\\s*${escape(transactionCount)}\\s*$`)),
      ).toBeVisible();
    } else {
      await expect(this.page.getByText('No merchant spending in this window.')).toBeVisible();
    }

    await expect(this.page.getByText(new RegExp(`^\\s*Top 5 ${escape(report.biggest.label)}\\s*$`, 'i'))).toBeVisible();
    if (report.biggest.first !== undefined) {
      const { merchant, amount } = report.biggest.first;
      const cells = this.biggestRows.first().getByRole('cell');
      await expect(cells.nth(0)).toHaveText(merchant);
      await expect(cells.nth(2)).toHaveText(amount);
    } else {
      await expect(this.biggestRows).toHaveCount(0);
    }

    const attention = `${report.needsAttention} transaction${report.needsAttention === 1 ? ' requires' : 's require'} attention`;
    await expect(this.page.getByText(attention, { exact: true })).toBeVisible();
    await expect(this.reviewNowLink).toBeVisible();
  }
}

function escape(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
