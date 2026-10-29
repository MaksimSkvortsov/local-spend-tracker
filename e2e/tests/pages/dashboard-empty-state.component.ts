import { Page, Locator, expect } from '@playwright/test';

export enum DashboardAction {
  ConfigureAi = 'Configure AI',
  ImportTransactions = 'Import Transactions',
}

export interface DashboardEmptyStateComponentAssertOptions {
  aiConfigured?: boolean;
  action?: DashboardAction;
}

/** The fresh-install onboarding panel (DashboardEmptyState): welcome text, Getting Started checklist, call-to-action. */
export class DashboardEmptyStateComponent {
  private readonly heading: Locator;
  private readonly introText: Locator;
  private readonly checklist: Locator;
  private readonly actionButton: Locator;

  constructor(private readonly page: Page) {
    this.heading = page.getByRole('heading', { name: 'Welcome to PeasantMoney', level: 1 });
    this.introText = page.getByText(
      'Set up private AI categorization, then import your first credit card statement to unlock local spending reports.',
    );
    this.checklist = page.getByLabel('Getting started checklist');
    this.actionButton = page.getByRole('main').getByRole('button', {
      name: new RegExp(`^(${Object.values(DashboardAction).join('|')})$`),
    });
  }

  async clickAction(action: DashboardAction) {
    await this.page.getByRole('main').getByRole('button', { name: action }).click();
  }

  async assert(options: DashboardEmptyStateComponentAssertOptions = {}) {
    await expect(this.heading).toBeVisible();
    await expect(this.introText).toBeVisible();
    await expect(this.checklist).toBeVisible();

    if (options.aiConfigured !== undefined) {
      await expect(this.checklistItem('Configure AI in Settings', options.aiConfigured)).toBeVisible();
      await expect(this.checklistItem('Upload the first statement in Import', false)).toBeVisible();
    }
    if (options.action !== undefined) {
      await expect(this.actionButton).toHaveCount(1);
      await expect(this.actionButton).toHaveText(new RegExp(`${options.action}$`));
    }
  }

  /** A checklist row, matched by its icon ligature, which is the only signal of its checked state. */
  private checklistItem(label: string, checked: boolean): Locator {
    // TODO: add data-testid and an accessible checked state for Getting Started checklist items
    const icon = checked ? 'check_box' : 'check_box_outline_blank';
    return this.checklist.getByText(new RegExp(`^${icon}\\s*${label}$`));
  }
}
