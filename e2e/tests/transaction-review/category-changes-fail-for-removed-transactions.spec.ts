// spec: specs/transaction-review.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { DashboardPage } from '../pages/dashboard.page';
import { TransactionsPage } from '../pages/transactions.page';
import { Cards } from '../test-data/cards';
import { Categories } from '../test-data/categories';
import { TransactionMessages } from '../test-data/transaction-messages';
import { TransactionRows } from '../test-data/transaction-rows';
import { Seeds } from '../test-data/seeds';

test.describe('Transaction review', () => {
  test('Category changes fail for transactions removed elsewhere', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const transactionsPage = new TransactionsPage(page);
    const { toolbar, table } = transactionsPage;

    // 1. Seed Seeds.ThreeCards, open Transactions, and choose Card "BofA Checking", then seed Seeds.OneCard through the harness without reloading
    await harness.seed(Seeds.ThreeCards);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Transactions);
    await toolbar.chooseCard(Cards.BofaChecking.name);
    await toolbar.assert({ transactionCount: 22 });
    await harness.seed(Seeds.OneCard);
    await toolbar.assert({ transactionCount: 22 });

    // 2. Choose "Other Card Payment" for the Dec 25, 2025 "ZELLE PAYMENT TO R LEE" row
    await table.chooseCategory(TransactionRows.ZelleDecember, Categories.OtherCardPayment);
    await transactionsPage.assert({ errorMessage: TransactionMessages.CategoryChangeNotFound });
    await toolbar.assert({ transactionCount: 20, card: Categories.AllCards });
    await table.assert({ cardsInRows: { shown: [Cards.ChaseSapphire.name], hidden: [Cards.BofaChecking.name] } });

    // 3. Seed Seeds.ThreeCards, open Dashboard and then Transactions from the navigation, seed Seeds.OneCard through the harness again, and click "Require Review"
    await harness.seed(Seeds.ThreeCards);
    await appShell.open(NavItem.Dashboard);
    await appShell.open(NavItem.Transactions);
    await toolbar.assert({ transactionCount: 62, reviewCount: 6 });
    await harness.seed(Seeds.OneCard);
    await toolbar.clickRequireReview();
    await toolbar.assert({ transactionCount: 6 });

    // 4. Click "Confirm" on the Dec 25, 2025 "ZELLE PAYMENT TO R LEE" row
    await table.clickConfirm(TransactionRows.ZelleDecember);
    await transactionsPage.assert({ errorMessage: TransactionMessages.ConfirmNotFound });
    await toolbar.assert({ transactionCount: 2, reviewCount: 2 });
    await table.assert({ rows: [TransactionRows.MarketStallDecember, TransactionRows.MarketStallJanuary] });

    // 5. Reload the page
    await transactionsPage.reload();
    await toolbar.assert({ transactionCount: 20, reviewCount: 2 });
  });
});
