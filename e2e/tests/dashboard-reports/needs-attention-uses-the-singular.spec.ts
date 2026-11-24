// spec: specs/dashboard-reports.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { DashboardPage } from '../pages/dashboard.page';
import { TransactionsPage } from '../pages/transactions.page';
import { TransactionRows } from '../test-data/transaction-rows';
import { Seeds } from '../test-data/seeds';

test.describe('Dashboard reports', () => {
  test('Needs Attention uses the singular for one transaction', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const transactionsPage = new TransactionsPage(page);
    const { toolbar, table } = transactionsPage;

    // 1. Seed Seeds.OneCard, open Transactions from the navigation, click "Require Review", and click "Confirm" on the Dec 23, 2025 "SQ *MARKET STALL 22" row
    await harness.seed(Seeds.OneCard);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Transactions);
    await toolbar.clickRequireReview();
    await table.clickConfirm(TransactionRows.MarketStallDecember);
    await table.assert({ rows: [TransactionRows.MarketStallJanuary] });

    // 2. Open Dashboard from the navigation
    await appShell.open(NavItem.Dashboard);
    await dashboardPage.reportCards.assert({ needsAttention: 1 });
  });
});
