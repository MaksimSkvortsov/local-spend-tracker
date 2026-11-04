// spec: specs/fresh-launch.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { DashboardPage } from '../pages/dashboard.page';
import { DashboardAction } from '../pages/dashboard-empty-state.component';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { SettingsPage } from '../pages/settings.page';
import { AiConnectionStatus } from '../pages/ai-configuration.component';
import { Seeds } from '../test-data/seeds';

test.describe('Fresh launch', () => {
  test('User deletes the AI configuration and onboarding resets', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const settingsPage = new SettingsPage(page);
    const aiConfiguration = settingsPage.aiConfiguration;

    // 1. Seed Seeds.AiConfigured and open the app root "/"
    await harness.seed(Seeds.AiConfigured);
    await dashboardPage.gotoAppRoot();
    await dashboardPage.assert({ isEmpty: true });
    await dashboardPage.emptyState.assert({ aiConfigured: true, action: DashboardAction.ImportTransactions });

    // 2. Open Settings from the navigation
    await appShell.open(NavItem.Settings);
    await settingsPage.assert();
    await aiConfiguration.assert({ status: AiConnectionStatus.Connected });
    await aiConfiguration.form.assert({ hasSavedKey: true });
    await aiConfiguration.actions.assert({ deleteConfigurationEnabled: true });

    // 3. Click "Delete Configuration"
    await aiConfiguration.actions.clickDeleteConfiguration();
    await aiConfiguration.assert({ configurationDeleted: true, status: AiConnectionStatus.NotConfigured });
    await aiConfiguration.form.assert({ hasSavedKey: false });
    await aiConfiguration.actions.assert({ deleteConfigurationEnabled: false });

    // 4. Open Dashboard from the navigation
    await appShell.open(NavItem.Dashboard);
    await dashboardPage.emptyState.assert({ aiConfigured: false, action: DashboardAction.ConfigureAi });
  });
});
