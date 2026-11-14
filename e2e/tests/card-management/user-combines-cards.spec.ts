// spec: specs/card-management.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { CardsPage } from '../pages/cards.page';
import { CombineCardsPopup } from '../pages/combine-cards.popup';
import { CombineCardsConfirmationPopup } from '../pages/combine-cards-confirmation.popup';
import { TransactionsPage } from '../pages/transactions.page';
import { ImportPage } from '../pages/import.page';
import { DashboardPage } from '../pages/dashboard.page';
import { Cards } from '../test-data/cards';
import { CardRows } from '../test-data/card-rows';
import { CardNames } from '../test-data/card-names';
import { CardMessages } from '../test-data/card-messages';
import { ImportHistoryRows } from '../test-data/import-history';
import { Seeds } from '../test-data/seeds';

test.describe('Card management', () => {
  test('User combines a suspected duplicate card', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const cardsPage = new CardsPage(page);
    const combinePopup = new CombineCardsPopup(page);
    const confirmPopup = new CombineCardsConfirmationPopup(page);
    const transactionsPage = new TransactionsPage(page);
    const importPage = new ImportPage(page);
    const table = cardsPage.table;
    const bofa = CardRows.BofaCheckingSeeded;
    const chase = CardRows.ChaseSapphireDuplicateSuspected;
    const backup = CardRows.ChaseSapphireBackupDuplicateSuspected;
    const duplicateCards = [bofa, chase, backup];
    const backupName = CardNames.ChaseSapphireBackup.saved;

    // 1. Seed Seeds.ThreeCards, open Cards, and rename CapOne Debit to "Chase Sapphire Backup"
    await harness.seed(Seeds.ThreeCards);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Cards);
    await table.clickRename(Cards.CapOneDebit.name);
    await table.fillRename(CardNames.ChaseSapphireBackup.entered);
    await table.clickSave();
    await cardsPage.assert({ message: CardMessages.Renamed, cards: duplicateCards });

    // 2. Click "Combine" on the Chase Sapphire Backup row
    await table.clickCombine(backupName);
    await combinePopup.assert({ source: backup, target: bofa, moves: { from: backup, to: bofa } });

    // 3. Choose "BofA Checking (22 transactions)" as the Source Card
    await combinePopup.chooseSource(bofa);
    await combinePopup.assert({ source: bofa, target: chase, moves: { from: bofa, to: chase } });

    // 4. Click "Cancel" in the Combine Cards dialog
    await combinePopup.clickCancel();
    await combinePopup.assert({ isClosed: true });
    await cardsPage.assert({ cards: duplicateCards });

    // 5. Click "Combine" on Chase Sapphire Backup again, choose "Chase Sapphire Backup (20 transactions)" as the Target Card
    await table.clickCombine(backupName);
    await combinePopup.chooseTarget(backup);
    await combinePopup.assert({ source: backup, target: bofa });

    // 6. Choose "Chase Sapphire (20 transactions)" as the Target Card and click "Combine Cards"
    await combinePopup.chooseTarget(chase);
    await combinePopup.assert({ moves: { from: backup, to: chase } });
    await combinePopup.clickCombineCards();
    await combinePopup.assert({ isClosed: true });
    await confirmPopup.assert({ combining: { source: backup, target: chase } });

    // 7. Click "Cancel" in the "Combine cards?" dialog
    await confirmPopup.clickCancel();
    await confirmPopup.assert({ isClosed: true });
    await combinePopup.assert({ source: backup, target: chase });
    await cardsPage.assert({ cards: duplicateCards });

    // 8. Click "Combine Cards" again and confirm with "Combine Cards" in the "Combine cards?" dialog
    await combinePopup.clickCombineCards();
    await confirmPopup.clickConfirm();
    await combinePopup.assert({ isClosed: true });
    await confirmPopup.assert({ isClosed: true });
    await cardsPage.assert({
      message: CardMessages.BackupCombinedIntoSapphire,
      cards: [bofa, CardRows.ChaseSapphireCombined],
    });

    // 9. Open Transactions from the navigation, then open Import from the navigation
    await appShell.open(NavItem.Transactions);
    await transactionsPage.assert({
      transactionCount: 62,
      cardFilterOptions: ['All cards', Cards.BofaChecking.name, Cards.ChaseSapphire.name],
    });
    await appShell.open(NavItem.Import);
    await importPage.assert();
    await importPage.history.assert({
      rows: [ImportHistoryRows.CapOneDecemberSeeded, ImportHistoryRows.CapOneJanuarySeeded],
    });
  });
});
