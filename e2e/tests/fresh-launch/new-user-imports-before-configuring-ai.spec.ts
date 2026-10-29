// spec: specs/fresh-launch.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { DashboardPage } from '../pages/dashboard.page';
import { DashboardAction } from '../pages/dashboard-empty-state.component';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { ImportPage } from '../pages/import.page';
import { CardsPage } from '../pages/cards.page';
import { Seeds } from '../test-data/seeds';
import { Statements } from '../test-data/statements';
import { ImportHistoryRows } from '../test-data/import-history';
import { CardRows } from '../test-data/card-rows';

test.describe('Fresh launch', () => {
  test('New user imports before configuring AI', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const importPage = new ImportPage(page);
    const cardsPage = new CardsPage(page);
    const statement = Statements.CapitalOneDecember;

    // 1. Seed Seeds.NewUser and open the app root "/"
    await harness.seed(Seeds.NewUser);
    await dashboardPage.gotoAppRoot();
    await dashboardPage.assert({ isEmpty: true });
    await dashboardPage.emptyState.assert({ aiConfigured: false, action: DashboardAction.ConfigureAi });

    // 2. Open Import from the navigation, select Statements.CapitalOneDecember through the harness, and click "browse files from your computer"
    await appShell.open(NavItem.Import);
    await importPage.assert();
    await importPage.uploadPrompt.assert();
    await harness.selectStatementFile(statement.absolutePath);
    await importPage.uploadPrompt.clickBrowseFiles();
    await importPage.preview.assert({ statement });

    // 3. Click "Confirm Import"
    await importPage.preview.clickConfirmImport();
    await importPage.preview.assert({ isMissingApiKeyError: true });
    await importPage.history.assert({ row: ImportHistoryRows.CapitalOneDecemberFailed });

    // 4. Open Cards, then Dashboard, from the navigation
    await appShell.open(NavItem.Cards);
    await cardsPage.assert({ singleCard: CardRows.CapitalOneAfterFailedImport });
    await appShell.open(NavItem.Dashboard);
    await dashboardPage.assert({ isEmpty: true });
    await dashboardPage.emptyState.assert({ aiConfigured: false, action: DashboardAction.ConfigureAi });
  });
});
