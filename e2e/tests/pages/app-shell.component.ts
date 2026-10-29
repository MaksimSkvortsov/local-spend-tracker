import { Page, Locator, expect } from '@playwright/test';

export enum NavItem {
  Dashboard = 'Dashboard',
  Import = 'Import',
  Transactions = 'Transactions',
  Rules = 'Rules',
  Cards = 'Cards',
  Settings = 'Settings',
}

/** The layout around every page (MainLayout): the sidebar with the brand link and primary navigation. */
export class AppShellComponent {
  private readonly sidebar: Locator;
  private readonly brandLink: Locator;
  private readonly navigation: Locator;

  constructor(page: Page) {
    this.sidebar = page.getByRole('complementary', { name: 'Primary navigation' });
    this.brandLink = this.sidebar.getByRole('link', { name: 'PeasantMoney dashboard' });
    this.navigation = this.sidebar.getByRole('navigation');
  }

  async open(item: NavItem) {
    await this.navLink(item).click();
  }

  async assert() {
    await expect(this.brandLink).toBeVisible();
    for (const item of Object.values(NavItem)) {
      await expect(this.navLink(item)).toBeVisible();
    }
  }

  private navLink(item: NavItem): Locator {
    return this.navigation.getByRole('link', { name: item, exact: true });
  }
}
