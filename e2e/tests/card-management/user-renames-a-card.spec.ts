// spec: specs/card-management.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { CardsPage } from '../pages/cards.page';
import { TransactionsPage } from '../pages/transactions.page';
import { DashboardPage } from '../pages/dashboard.page';
import { Cards } from '../test-data/cards';
import { CardRows } from '../test-data/card-rows';
import { CardNames } from '../test-data/card-names';
import { CardMessages } from '../test-data/card-messages';
import { Seeds } from '../test-data/seeds';

test.describe('Card management', () => {
  test('User renames a card and sees the new name everywhere', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const cardsPage = new CardsPage(page);
    const transactionsPage = new TransactionsPage(page);
    const dashboardPage = new DashboardPage(page);
    const table = cardsPage.table;
    const seededCards = [CardRows.BofaCheckingSeeded, CardRows.CapOneDebitSeeded, CardRows.ChaseSapphireSeeded];
    const renamedCards = [CardRows.BofaCheckingSeeded, CardRows.CapOneDebitSeeded, CardRows.SapphirePreferredSeeded];

    // 1. Seed Seeds.ThreeCards and open Cards from the navigation
    await harness.seed(Seeds.ThreeCards);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Cards);
    await cardsPage.assert({ cards: seededCards });

    // 2. Click "Rename" on the Chase Sapphire row, change the name to "Draft", then click "Rename" on the BofA Checking row
    await table.clickRename(Cards.ChaseSapphire.name);
    await table.assert({ editing: { value: Cards.ChaseSapphire.name } });
    await table.fillRename(CardNames.Draft.entered);
    await table.clickRename(Cards.BofaChecking.name);
    await table.assert({ editing: { value: Cards.BofaChecking.name }, cards: seededCards });

    // 3. Click "Cancel"
    await table.clickCancel();
    await table.assert({ idle: true, cards: seededCards });

    // 4. Click "Rename" on Chase Sapphire, enter only spaces, and click "Save"
    await table.clickRename(Cards.ChaseSapphire.name);
    await table.fillRename(CardNames.OnlySpaces.entered);
    await table.clickSave();
    await cardsPage.assert({ message: CardMessages.NameRequired });
    await table.assert({ editing: { value: CardNames.OnlySpaces.entered } });

    // 5. Enter "capone debit" (another card's name in different case) and click "Save"
    await table.fillRename(CardNames.SameAsCapOneDebit.entered);
    await table.clickSave();
    await cardsPage.assert({ message: CardMessages.NameTaken });
    await table.assert({ editing: { value: CardNames.SameAsCapOneDebit.entered } });

    // 6. Click "Cancel"
    await table.clickCancel();
    await table.assert({ idle: true, cards: seededCards });

    // 7. Click "Rename" on Chase Sapphire, enter "  Sapphire Preferred  " with surrounding spaces, and click "Save"
    await table.clickRename(Cards.ChaseSapphire.name);
    await table.fillRename(CardNames.SapphirePreferred.entered);
    await table.clickSave();
    await cardsPage.assert({ message: CardMessages.Renamed, cards: renamedCards });
    await table.assert({ idle: true });

    // 8. Open Transactions from the navigation and open the "Card" filter
    await appShell.open(NavItem.Transactions);
    await transactionsPage.assert({
      cardFilterOptions: ['All cards', ...renamedCards.map(card => card.name)],
      cardsInRows: { shown: [CardNames.SapphirePreferred.saved], hidden: [Cards.ChaseSapphire.name] },
    });

    // 9. Open Dashboard from the navigation, then open Cards and reload the page
    await appShell.open(NavItem.Dashboard);
    await dashboardPage.assert({
      cardsInFrequency: { shown: [CardNames.SapphirePreferred.saved], hidden: [Cards.ChaseSapphire.name] },
    });
    await appShell.open(NavItem.Cards);
    await cardsPage.reload();
    await cardsPage.assert({ cards: renamedCards });

    // 10. Click "Rename" on Sapphire Preferred, enter "capone debit", click "Save", then click "Cancel"
    await table.clickRename(CardNames.SapphirePreferred.saved);
    await table.fillRename(CardNames.SameAsCapOneDebit.entered);
    await table.clickSave();
    await cardsPage.assert({ message: CardMessages.NameTaken });
    await table.clickCancel();
    await table.assert({ idle: true, cards: renamedCards });
    await cardsPage.assert({ absentMessage: CardMessages.NameTaken });
  });
});
