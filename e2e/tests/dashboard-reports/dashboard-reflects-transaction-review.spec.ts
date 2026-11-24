// spec: specs/dashboard-reports.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { DashboardPage } from '../pages/dashboard.page';
import { TransactionsPage } from '../pages/transactions.page';
import { Categories } from '../test-data/categories';
import { DashboardPeriods } from '../test-data/dashboard-periods';
import { OneCardCategorySpending } from '../test-data/dashboard-reports';
import { TransactionRows } from '../test-data/transaction-rows';
import { Seeds } from '../test-data/seeds';

test.describe('Dashboard reports', () => {
  test('Dashboard reflects transaction review', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const transactionsPage = new TransactionsPage(page);
    const { period, reportCards } = dashboardPage;
    const { toolbar, table } = transactionsPage;

    // 1. Seed Seeds.OneCard, open Dashboard, click "Month", and choose Year "2025" and Month "December"
    await harness.seed(Seeds.OneCard);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Dashboard);
    await period.clickMonth();
    await period.chooseYear(DashboardPeriods.December2025.year);
    await period.chooseMonth(DashboardPeriods.December2025.month);
    await reportCards.assert({ categorySpending: OneCardCategorySpending.December2025, needsAttention: 2 });

    // 2. Click "Review now"
    await reportCards.clickReviewNow();
    await transactionsPage.assert();

    // 3. Click "Require Review" and choose "Groceries" for the Dec 23, 2025 "SQ *MARKET STALL 22" row, then open Dashboard from the navigation
    await toolbar.clickRequireReview();
    await table.chooseCategory(TransactionRows.MarketStallDecember, Categories.Groceries);
    await table.assert({ rows: [TransactionRows.MarketStallJanuary] });
    await appShell.open(NavItem.Dashboard);
    await period.assert({ selects: 'yearAndMonth', selected: DashboardPeriods.December2025 });
    await reportCards.assert({ categorySpending: OneCardCategorySpending.December2025MarketStallInGroceries });

    // 4. Click "Review now", click "Require Review", click "Confirm" on the Jan 23, 2026 "SQ *MARKET STALL 22" row, then open Dashboard from the navigation
    await reportCards.clickReviewNow();
    await toolbar.clickRequireReview();
    await table.clickConfirm(TransactionRows.MarketStallJanuary);
    await appShell.open(NavItem.Dashboard);
    await reportCards.assert({ needsAttention: 0 });
  });
});
