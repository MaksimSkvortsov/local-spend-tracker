// spec: specs/card-management.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { CardsPage } from '../pages/cards.page';
import { DeleteCardPopup } from '../pages/delete-card.popup';
import { TransactionsPage } from '../pages/transactions.page';
import { ImportPage } from '../pages/import.page';
import { DashboardPage } from '../pages/dashboard.page';
import { RulesPage } from '../pages/rules.page';
import { SettingsPage } from '../pages/settings.page';
import { AiConnectionStatus } from '../pages/ai-configuration.component';
import { Cards } from '../test-data/cards';
import { CardRows } from '../test-data/card-rows';
import { CardMessages } from '../test-data/card-messages';
import { ImportHistoryRows } from '../test-data/import-history';
import { RuleRows } from '../test-data/rule-rows';
import { Seeds } from '../test-data/seeds';

test.describe('Card management', () => {
  test('User deletes a card and its data', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const cardsPage = new CardsPage(page);
    const deletePopup = new DeleteCardPopup(page);
    const transactionsPage = new TransactionsPage(page);
    const importPage = new ImportPage(page);
    const rulesPage = new RulesPage(page);
    const settingsPage = new SettingsPage(page);
    const table = cardsPage.table;
    const allCards = [CardRows.BofaCheckingSeeded, CardRows.CapOneDebitSeeded, CardRows.ChaseSapphireSeeded];
    const remainingCards = [CardRows.BofaCheckingSeeded, CardRows.ChaseSapphireSeeded];

    // 1. Seed Seeds.ThreeCards, open Cards, and click "Delete" on the CapOne Debit row
    await harness.seed(Seeds.ThreeCards);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Cards);
    await table.clickDelete(Cards.CapOneDebit.name);
    await deletePopup.assert({ cardName: Cards.CapOneDebit.name });

    // 2. Click "Cancel"
    await deletePopup.clickCancel();
    await deletePopup.assert({ isClosed: true });
    await cardsPage.assert({ cards: allCards });

    // 3. Click "Delete" on CapOne Debit again and click "Delete Card"
    await table.clickDelete(Cards.CapOneDebit.name);
    await deletePopup.clickDeleteCard();
    await deletePopup.assert({ isClosed: true });
    await cardsPage.assert({ message: CardMessages.CapOneDebitDeleted, cards: remainingCards });

    // 4. Open Transactions, then Import, from the navigation
    await appShell.open(NavItem.Transactions);
    await transactionsPage.assert({
      transactionCount: 42,
      cardFilterOptions: ['All cards', Cards.BofaChecking.name, Cards.ChaseSapphire.name],
    });
    await appShell.open(NavItem.Import);
    await importPage.assert();
    await importPage.history.assert({
      rows: [
        ImportHistoryRows.BofaDecemberSeeded,
        ImportHistoryRows.BofaJanuarySeeded,
        ImportHistoryRows.ChaseSapphireDecemberSeeded,
        ImportHistoryRows.ChaseSapphireJanuarySeeded,
      ],
      absentFileNames: [ImportHistoryRows.CapOneDecemberSeeded.fileName, ImportHistoryRows.CapOneJanuarySeeded.fileName],
    });

    // 5. Open Dashboard from the navigation, click "All", then open Rules and Settings from the navigation
    await appShell.open(NavItem.Dashboard);
    await dashboardPage.clickAllReportMode();
    await dashboardPage.assert({
      reports: { transactionCount: 42 },
      cardsInFrequency: { shown: [Cards.BofaChecking.name, Cards.ChaseSapphire.name], hidden: [Cards.CapOneDebit.name] },
    });
    await appShell.open(NavItem.Rules);
    await rulesPage.assert({ rule: RuleRows.Uber });
    await appShell.open(NavItem.Settings);
    await settingsPage.assert();
    await settingsPage.aiConfiguration.assert({ status: AiConnectionStatus.Connected });
  });
});
