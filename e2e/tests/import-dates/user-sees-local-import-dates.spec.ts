// spec: specs/import-dates.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { CardsPage } from '../pages/cards.page';
import { ImportPage } from '../pages/import.page';
import { Seeds } from '../test-data/seeds';
import { CardRows } from '../test-data/card-rows';
import { ImportHistoryRows } from '../test-data/import-history';

test.describe('Import dates', () => {
  test('User sees import dates in local time on Cards and Import History', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const cardsPage = new CardsPage(page);
    const importPage = new ImportPage(page);

    // 1. Seed Seeds.OneCard and open Cards
    await harness.seed(Seeds.OneCard);
    await cardsPage.goto();
    await cardsPage.assert({ singleCard: CardRows.ChaseSapphireSeeded });

    // 2. Open Import from the navigation
    await appShell.open(NavItem.Import);
    await importPage.assert();
    await importPage.history.assert({
      rows: [ImportHistoryRows.ChaseSapphireDecemberSeeded, ImportHistoryRows.ChaseSapphireJanuarySeeded],
    });
  });
});
