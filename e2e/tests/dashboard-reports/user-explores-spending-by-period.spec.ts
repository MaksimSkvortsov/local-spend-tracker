// spec: specs/dashboard-reports.plan.md
// seed: tests/seed.spec.ts
import { test } from '@playwright/test';
import { HarnessHelper } from '../pages/harness.helper';
import { AppShellComponent, NavItem } from '../pages/app-shell.component';
import { DashboardPage } from '../pages/dashboard.page';
import { DashboardPeriods } from '../test-data/dashboard-periods';
import { DashboardReports } from '../test-data/dashboard-reports';
import { Seeds } from '../test-data/seeds';

test.describe('Dashboard reports', () => {
  test('User explores spending by period', async ({ page, request }) => {
    const harness = new HarnessHelper(request);
    const appShell = new AppShellComponent(page);
    const dashboardPage = new DashboardPage(page);
    const { period, reportCards } = dashboardPage;

    // 1. Seed Seeds.ThreeCards, open Dashboard from the navigation, and click "All"
    await harness.seed(Seeds.ThreeCards);
    await dashboardPage.gotoAppRoot();
    await appShell.open(NavItem.Dashboard);
    await period.clickAll();
    await period.assert({ selects: 'none' });
    await reportCards.assert({ report: DashboardReports.AllTime });

    // 2. Click "Month", then choose Year "2025" and Month "December"
    await period.clickMonth();
    await period.chooseYear(DashboardPeriods.December2025.year);
    await period.chooseMonth(DashboardPeriods.December2025.month);
    await period.assert({
      selects: 'yearAndMonth',
      yearOptions: DashboardPeriods.YearOptions,
      monthOptions: DashboardPeriods.MonthOptions,
    });
    await reportCards.assert({ report: DashboardReports.December2025 });

    // 3. Click "Year" and choose "2026"
    await period.clickYear();
    await period.chooseYear(DashboardPeriods.Year2026);
    await period.assert({ selects: 'year' });
    await reportCards.assert({ report: DashboardReports.Year2026 });

    // 4. Click "Month", then choose Year "2025" and Month "March"
    await period.clickMonth();
    await period.chooseYear(DashboardPeriods.March2025.year);
    await period.chooseMonth(DashboardPeriods.March2025.month);
    await reportCards.assert({ report: DashboardReports.March2025 });

    // 5. Open Cards from the navigation, open Dashboard again, then reload the page
    await appShell.open(NavItem.Cards);
    await appShell.open(NavItem.Dashboard);
    await period.assert({ selects: 'yearAndMonth', selected: DashboardPeriods.March2025 });
    await reportCards.assert({ report: DashboardReports.March2025 });
    await dashboardPage.reload();
    await period.assert({ selects: 'yearAndMonth', selected: DashboardPeriods.March2025 });
    await reportCards.assert({ report: DashboardReports.March2025 });
  });
});
