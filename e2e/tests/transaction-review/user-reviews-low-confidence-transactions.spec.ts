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
  test('User reviews low-confidence transactions', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const transactionsPage = new TransactionsPage(page);
    const { toolbar, table } = transactionsPage;

    // 1. Seed Seeds.ThreeCards and open Transactions from the navigation
    await harness.seed(Seeds.ThreeCards);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Transactions);
    await transactionsPage.assert();
    await toolbar.assert({ transactionCount: 62, reviewCount: 6 });
    await table.assert({ footer: { shown: 10, total: 62 }, previousPageEnabled: false });

    // 2. Click "Require Review"
    await toolbar.clickRequireReview();
    await toolbar.assert({ transactionCount: 6 });
    await table.assert({
      footer: { shown: 6, total: 6 },
      rows: [
        TransactionRows.MarketStallDecember,
        TransactionRows.VenmoDecember,
        TransactionRows.ZelleDecember,
        TransactionRows.MarketStallJanuary,
        TransactionRows.VenmoJanuary,
        TransactionRows.ZelleJanuary,
      ],
    });

    // 3. Click "Confirm" on the Dec 23, 2025 "SQ *MARKET STALL 22" row
    await table.clickConfirm(TransactionRows.MarketStallDecember);
    await toolbar.assert({ transactionCount: 5, reviewCount: 5 });
    await table.assert({
      rows: [
        TransactionRows.VenmoDecember,
        TransactionRows.ZelleDecember,
        TransactionRows.MarketStallJanuary,
        TransactionRows.VenmoJanuary,
        TransactionRows.ZelleJanuary,
      ],
    });

    // 4. Choose "Shopping" for the Dec 24, 2025 "VENMO *J SMITH" row
    await table.chooseCategory(TransactionRows.VenmoDecember, Categories.Shopping);
    await toolbar.assert({ transactionCount: 4, reviewCount: 4 });
    await table.assert({
      rows: [
        TransactionRows.ZelleDecember,
        TransactionRows.MarketStallJanuary,
        TransactionRows.VenmoJanuary,
        TransactionRows.ZelleJanuary,
      ],
    });

    // 5. Click "All", choose Category "Shopping" and Card "CapOne Debit"
    await toolbar.clickAll();
    await toolbar.chooseCategory(Categories.Shopping);
    await toolbar.chooseCard(Cards.CapOneDebit.name);
    await toolbar.assert({ transactionCount: 3 });
    await table.assert({
      rows: [
        TransactionRows.TargetRefundDecember,
        TransactionRows.VenmoDecemberShopping,
        TransactionRows.TargetRefundJanuary,
      ],
    });

    // 6. Choose Category "Other" and Card "Chase Sapphire"
    await toolbar.chooseCategory(Categories.Other);
    await toolbar.chooseCard(Cards.ChaseSapphire.name);
    await toolbar.assert({ transactionCount: 2 });
    await table.assert({
      rows: [TransactionRows.MarketStallDecemberConfirmed, TransactionRows.MarketStallJanuary],
    });

    // 7. Reload the page
    await transactionsPage.reload();
    await toolbar.assert({
      transactionCount: 62,
      reviewCount: 4,
      category: Categories.AllCategories,
      card: Categories.AllCards,
    });
  });
});
