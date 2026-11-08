// spec: specs/card-management.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { CardsPage } from '../pages/cards.page';
import { DashboardPage } from '../pages/dashboard.page';
import { Cards } from '../test-data/cards';
import { CardRows } from '../test-data/card-rows';
import { CardNames } from '../test-data/card-names';
import { CardMessages } from '../test-data/card-messages';
import { CardSearches } from '../test-data/card-searches';
import { Seeds } from '../test-data/seeds';

test.describe('Card management', () => {
  test('User searches cards and renames a card out of the filter', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const cardsPage = new CardsPage(page);
    const dashboardPage = new DashboardPage(page);
    const table = cardsPage.table;
    const allCards = { cardCount: 3, transactionCount: 62 };

    // 1. Seed Seeds.ThreeCards, open Cards, and type "SAPPHIRE" in "Search cards by name..."
    await harness.seed(Seeds.ThreeCards);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Cards);
    await table.search(CardSearches.Sapphire);
    await cardsPage.assert({ summary: allCards, cards: [CardRows.ChaseSapphireSeeded] });

    // 2. Replace the search with "amex"
    await table.search(CardSearches.NoMatch);
    await cardsPage.assert({ summary: allCards, cards: [] });
    await table.assert({ noCardsFound: true });

    // 3. Replace the search with "capone", click "Rename" on CapOne Debit, enter "Everyday Debit", and click "Save"
    await table.search(CardSearches.CapOne);
    await table.assert({ cards: [CardRows.CapOneDebitSeeded] });
    await table.clickRename(Cards.CapOneDebit.name);
    await table.fillRename(CardNames.EverydayDebit.entered);
    await table.clickSave();
    await cardsPage.assert({ message: CardMessages.Renamed, summary: allCards, cards: [] });
    await table.assert({ noCardsFound: true, searchFocused: true });

    // 4. Clear the search
    await table.search(CardSearches.Cleared);
    await cardsPage.assert({
      cards: [CardRows.BofaCheckingSeeded, CardRows.ChaseSapphireSeeded, CardRows.EverydayDebitSeeded],
    });
    await table.assert({ noCardsFound: false });
  });
});
