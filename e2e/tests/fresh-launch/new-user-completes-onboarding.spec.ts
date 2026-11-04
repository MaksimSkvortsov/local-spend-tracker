// spec: specs/fresh-launch.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { DashboardPage } from '../pages/dashboard.page';
import { DashboardAction } from '../pages/dashboard-empty-state.component';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { TransactionsPage } from '../pages/transactions.page';
import { CardsPage } from '../pages/cards.page';
import { RulesPage } from '../pages/rules.page';
import { ImportPage } from '../pages/import.page';
import { SettingsPage } from '../pages/settings.page';
import { AiConnectionStatus } from '../pages/ai-configuration.component';
import { Seeds } from '../test-data/seeds';
import { AiSettingsData } from '../test-data/ai-settings';
import { Statements } from '../test-data/statements';
import { ImportHistoryRows } from '../test-data/import-history';
import { CardRows } from '../test-data/card-rows';

test.describe('Fresh launch', () => {
  test('New user completes onboarding from the dashboard', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const transactionsPage = new TransactionsPage(page);
    const cardsPage = new CardsPage(page);
    const rulesPage = new RulesPage(page);
    const importPage = new ImportPage(page);
    const settingsPage = new SettingsPage(page);
    const aiConfiguration = settingsPage.aiConfiguration;
    const statement = Statements.CapitalOneDecember;

    // 1. Seed Seeds.NewUser and open the app root "/"
    await harness.seed(Seeds.NewUser);
    await dashboardPage.gotoAppRoot();
    await dashboardPage.assert({ isEmpty: true });
    await dashboardPage.emptyState.assert({ aiConfigured: false, action: DashboardAction.ConfigureAi });
    await appShell.assert();

    // 2. Use the primary navigation to open Transactions, Cards, Rules, and Import in turn
    await appShell.open(NavItem.Transactions);
    await transactionsPage.assert({ isEmpty: true });
    await appShell.open(NavItem.Cards);
    await cardsPage.assert({ isEmpty: true });
    await appShell.open(NavItem.Rules);
    await rulesPage.assert({ isEmpty: true });
    await appShell.open(NavItem.Import);
    await importPage.assert();
    await importPage.uploadPrompt.assert();
    await importPage.history.assert({ isEmpty: true });

    // 3. Open Dashboard from the navigation and click "Configure AI"
    await appShell.open(NavItem.Dashboard);
    await dashboardPage.emptyState.assert({ aiConfigured: false, action: DashboardAction.ConfigureAi });
    await dashboardPage.emptyState.clickAction(DashboardAction.ConfigureAi);
    await settingsPage.assert();
    await aiConfiguration.assert({ status: AiConnectionStatus.NotConfigured });
    await aiConfiguration.form.assert({ formData: AiSettingsData.Defaults, hasSavedKey: false });
    await aiConfiguration.actions.assert({ deleteConfigurationEnabled: false });

    // 4. Enter AiSettingsData.FakeKey in "OpenAI API Key *" and click "Test Connection"
    await aiConfiguration.form.fillApiKey(AiSettingsData.FakeKey.apiKey);
    await aiConfiguration.actions.clickTestConnection();
    await aiConfiguration.assert({ connectionSucceeded: true, status: AiConnectionStatus.NotConfigured });

    // 5. Click "Save"
    await aiConfiguration.actions.clickSave();
    await aiConfiguration.assert({ status: AiConnectionStatus.Connected });
    await aiConfiguration.form.assert({ hasSavedKey: true });
    await aiConfiguration.actions.assert({ deleteConfigurationEnabled: true });

    // 6. Open Dashboard from the navigation, then reload the page
    await appShell.open(NavItem.Dashboard);
    await dashboardPage.emptyState.assert({ aiConfigured: true, action: DashboardAction.ImportTransactions });
    await dashboardPage.reload();
    await dashboardPage.emptyState.assert({ aiConfigured: true, action: DashboardAction.ImportTransactions });

    // 7. Click "Import Transactions"
    await dashboardPage.emptyState.clickAction(DashboardAction.ImportTransactions);
    await importPage.assert();
    await importPage.uploadPrompt.assert();
    await importPage.history.assert({ isEmpty: true });

    // 8. Select Statements.CapitalOneDecember through the harness and click "browse files from your computer"
    await harness.selectStatementFile(statement.absolutePath);
    await importPage.uploadPrompt.clickBrowseFiles();
    await importPage.preview.assert({ statement });

    // 9. Click "Cancel", then click "browse files from your computer" again
    await importPage.preview.clickCancel();
    await importPage.uploadPrompt.assert();
    await importPage.history.assert({ isEmpty: true });
    await importPage.uploadPrompt.clickBrowseFiles();
    await importPage.preview.assert({ statement });

    // 10. Click "Confirm Import"
    await importPage.preview.clickConfirmImport();
    await importPage.uploadPrompt.assert();
    await importPage.history.assert({ row: ImportHistoryRows.CapitalOneDecemberCompleted });

    // 11. Open Dashboard from the navigation
    await appShell.open(NavItem.Dashboard);
    await dashboardPage.assert({ reports: { transactionCount: statement.transactionCount } });

    // 12. Open Cards from the navigation
    await appShell.open(NavItem.Cards);
    await cardsPage.assert({ singleCard: CardRows.CapitalOneImported });
  });
});
