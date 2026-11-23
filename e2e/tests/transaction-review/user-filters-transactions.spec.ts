// spec: specs/transaction-review.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { DashboardPage } from '../pages/dashboard.page';
import { TransactionsPage } from '../pages/transactions.page';
import { Cards } from '../test-data/cards';
import { Categories } from '../test-data/categories';
import { TransactionRows } from '../test-data/transaction-rows';
import { Seeds } from '../test-data/seeds';

test.describe('Transaction review', () => {
  test('User filters and pages through transactions', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const transactionsPage = new TransactionsPage(page);
    const { toolbar, table } = transactionsPage;

    // 1. Seed Seeds.ThreeCards, open Transactions, and click "Next page" six times
    await harness.seed(Seeds.ThreeCards);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Transactions);
    await transactionsPage.assert();
    for (let click = 1; click <= 6; click++) {
      await table.clickNextPage();
      await table.assert({ footer: { shown: click < 6 ? 10 : 2, total: 62 }, previousPageEnabled: true });
    }
    await table.assert({ footer: { shown: 2, total: 62 }, nextPageEnabled: false });

    // 2. Click "Previous page"
    await table.clickPreviousPage();
    await table.assert({ footer: { shown: 10, total: 62 }, previousPageEnabled: true, nextPageEnabled: true });

    // 3. Choose Card "BofA Checking"
    await toolbar.chooseCard(Cards.BofaChecking.name);
    await table.assert({
      footer: { shown: 10, total: 22 },
      previousPageEnabled: false,
      firstRow: TransactionRows.DirectDepositDecember,
    });

    // 4. Choose Category "Travel"
    await toolbar.chooseCategory(Categories.Travel);
    await toolbar.assert({ transactionCount: 0 });
    await table.assert({ noMatches: true, footer: null });

    // 5. Choose Card "All cards"
    await toolbar.chooseCard(Categories.AllCards);
    await toolbar.assert({ transactionCount: 2 });
    await table.assert({ rows: [TransactionRows.DeltaDecember, TransactionRows.DeltaJanuary] });

    // 6. Click "Require Review"
    await toolbar.clickRequireReview();
    await toolbar.assert({ transactionCount: 0 });
    await table.assert({ noMatches: true });
  });
});
