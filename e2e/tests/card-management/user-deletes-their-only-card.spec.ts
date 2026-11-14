// spec: specs/card-management.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { CardsPage } from '../pages/cards.page';
import { DeleteCardPopup } from '../pages/delete-card.popup';
import { DashboardPage } from '../pages/dashboard.page';
import { DashboardAction } from '../pages/dashboard-empty-state.component';
import { RulesPage } from '../pages/rules.page';
import { Cards } from '../test-data/cards';
import { CardRows } from '../test-data/card-rows';
import { CardMessages } from '../test-data/card-messages';
import { RuleRows } from '../test-data/rule-rows';
import { Seeds } from '../test-data/seeds';

test.describe('Card management', () => {
  test('User deletes their only card and onboarding returns', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const cardsPage = new CardsPage(page);
    const deletePopup = new DeleteCardPopup(page);
    const rulesPage = new RulesPage(page);
    const table = cardsPage.table;

    // 1. Seed Seeds.OneCard and open Cards from the navigation
    await harness.seed(Seeds.OneCard);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Cards);
    await cardsPage.assert({ singleCard: CardRows.ChaseSapphireSeeded });
    await table.assert({ combineDisabledFor: Cards.ChaseSapphire.name });

    // 2. Click "Delete" on Chase Sapphire and confirm with "Delete Card"
    await table.clickDelete(Cards.ChaseSapphire.name);
    await deletePopup.assert({ cardName: Cards.ChaseSapphire.name });
    await deletePopup.clickDeleteCard();
    await deletePopup.assert({ isClosed: true });
    await cardsPage.assert({ message: CardMessages.ChaseSapphireDeleted, isEmpty: true });

    // 3. Open Dashboard from the navigation
    await appShell.open(NavItem.Dashboard);
    await dashboardPage.assert({ isEmpty: true });
    await dashboardPage.emptyState.assert({ aiConfigured: true, action: DashboardAction.ImportTransactions });

    // 4. Open Rules from the navigation
    await appShell.open(NavItem.Rules);
    await rulesPage.assert({ rule: RuleRows.UberWithNoMatches });
  });
});
