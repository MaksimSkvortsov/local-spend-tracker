import { Page, Locator, expect } from '@playwright/test';
import { CategorySpending, DashboardReport } from '../test-data/dashboard-reports';

export interface DashboardReportsComponentAssertOptions {
  /** No report is shown, as on the onboarding dashboard. */
  absent?: boolean;
  /** Only the number of transactions in the window. */
  transactionCount?: number;
  /** Everything the dashboard reports for the window. */
  report?: DashboardReport;
  /** The category chart total and some of its lines. */
  categorySpending?: CategorySpending;
  /** Needs Attention for this many transactions; 0 means all transactions look good. */
  needsAttention?: number;
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

  async clickReviewNow() {
    await this.reviewNowLink.click();
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
    if (options.categorySpending !== undefined) {
      await this.assertCategorySpending(options.categorySpending);
    }
    if (options.needsAttention !== undefined) {
      await this.assertNeedsAttention(options.needsAttention);
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

    await this.assertNeedsAttention(report.needsAttention);
  }

  private async assertCategorySpending(spending: CategorySpending) {
    await expect(this.categoryTotal).toHaveText(new RegExp(`^\\s*Total\\s*${escape(spending.total)}\\s*$`));
    for (const { name, amount } of spending.lines) {
      await expect(
        this.page.getByText(new RegExp(`^\\s*${escape(name)}\\s*${escape(amount)}\\s*$`)),
      ).toBeVisible();
    }
    for (const name of spending.absent ?? []) {
      await expect(this.page.getByText(new RegExp(`^\\s*${escape(name)}\\s*\\$`))).toHaveCount(0);
    }
  }

  private async assertNeedsAttention(count: number) {
    if (count === 0) {
      await expect(this.page.getByText('All transactions look good', { exact: true })).toBeVisible();
      await expect(this.page.getByText('No low-confidence categorizations found.', { exact: true })).toBeVisible();
      await expect(this.reviewNowLink).toBeHidden();
      return;
    }

    const summary = `${count} transaction${count === 1 ? ' requires' : 's require'} attention`;
    await expect(this.page.getByText(summary, { exact: true })).toBeVisible();
    await expect(this.reviewNowLink).toBeVisible();
  }
}

function escape(text: string): string {
  return text.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
