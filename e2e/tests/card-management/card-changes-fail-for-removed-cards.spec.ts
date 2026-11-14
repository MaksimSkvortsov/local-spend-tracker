// spec: specs/card-management.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { CardsPage } from '../pages/cards.page';
import { CombineCardsPopup } from '../pages/combine-cards.popup';
import { CombineCardsConfirmationPopup } from '../pages/combine-cards-confirmation.popup';
import { DeleteCardPopup } from '../pages/delete-card.popup';
import { DashboardPage } from '../pages/dashboard.page';
import { Cards } from '../test-data/cards';
import { CardRows } from '../test-data/card-rows';
import { CardNames } from '../test-data/card-names';
import { CardMessages } from '../test-data/card-messages';
import { Seeds } from '../test-data/seeds';

test.describe('Card management', () => {
  test('Card changes fail when the cards were changed elsewhere', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const cardsPage = new CardsPage(page);
    const combinePopup = new CombineCardsPopup(page);
    const confirmPopup = new CombineCardsConfirmationPopup(page);
    const deletePopup = new DeleteCardPopup(page);
    const table = cardsPage.table;
    const staleCards = [CardRows.BofaCheckingSeeded, CardRows.CapOneDebitSeeded, CardRows.ChaseSapphireSeeded];
    const remainingCards = [CardRows.ChaseSapphireSeeded];

    // 1. Seed Seeds.ThreeCards and open Cards from the navigation, then seed Seeds.OneCard through the harness without reloading, so CapOne Debit and BofA Checking no longer exist
    await harness.seed(Seeds.ThreeCards);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Cards);
    await cardsPage.assert({ cards: staleCards });
    await harness.seed(Seeds.OneCard);
    await cardsPage.assert({ cards: staleCards });

    // 2. Click "Rename" on CapOne Debit, enter "Everyday Debit", and click "Save"
    await table.clickRename(Cards.CapOneDebit.name);
    await table.fillRename(CardNames.EverydayDebit.entered);
    await table.clickSave();
    await cardsPage.assert({ message: CardMessages.CardNotFound, cards: remainingCards });
    await table.assert({ idle: true });

    // 3. Seed Seeds.ThreeCards, open Dashboard and then Cards from the navigation, and seed Seeds.OneCard through the harness again
    await harness.seed(Seeds.ThreeCards);
    await appShell.open(NavItem.Dashboard);
    await appShell.open(NavItem.Cards);
    await cardsPage.assert({ cards: staleCards });
    await harness.seed(Seeds.OneCard);
    await cardsPage.assert({ cards: staleCards });

    // 4. Click "Combine" on CapOne Debit, choose "Chase Sapphire (20 transactions)" as the Target Card, click "Combine Cards", and confirm with "Combine Cards"
    await table.clickCombine(Cards.CapOneDebit.name);
    await combinePopup.chooseTarget(CardRows.ChaseSapphireSeeded);
    await combinePopup.clickCombineCards();
    await confirmPopup.clickConfirm();
    await combinePopup.assert({ isClosed: true });
    await confirmPopup.assert({ isClosed: true });
    await cardsPage.assert({ message: CardMessages.CardNotFound, cards: remainingCards });

    // 5. Seed Seeds.ThreeCards, open Dashboard and then Cards from the navigation, and seed Seeds.OneCard through the harness again
    await harness.seed(Seeds.ThreeCards);
    await appShell.open(NavItem.Dashboard);
    await appShell.open(NavItem.Cards);
    await cardsPage.assert({ cards: staleCards });
    await harness.seed(Seeds.OneCard);
    await cardsPage.assert({ cards: staleCards });

    // 6. Click "Delete" on BofA Checking and click "Delete Card"
    await table.clickDelete(Cards.BofaChecking.name);
    await deletePopup.clickDeleteCard();
    await deletePopup.assert({ isClosed: true });
    await cardsPage.assert({ message: CardMessages.CardNotFound, cards: remainingCards });

    // 7. Reload the page
    await cardsPage.reload();
    await cardsPage.assert({ cards: remainingCards });
  });
});
