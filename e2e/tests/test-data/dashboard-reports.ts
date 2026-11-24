/** What the dashboard reports for one window. Amounts and labels are as shown. */
export interface DashboardReport {
  window: string;
  totalSpend: string;
  transactionCount: number;
  /** The spending chart's total and its largest category; both are left out when the window has no spending. */
  categories?: { total: string; largest: { name: string; amount: string } };
  /** The first Top Merchants row; left out when the window has no spending. */
  topMerchant?: { name: string; amount: string; transactionCount: string };
  /** The window label after "Top 5" in Biggest Transactions, and its first row when there is one. */
  biggest: { label: string; first?: { merchant: string; amount: string } };
  /** Transactions needing review, counted over every window. */
  needsAttention: number;
}

/** The category chart of a window: its total, some of its lines, and categories it does not list. */
export interface CategorySpending {
  total: string;
  lines: { name: string; amount: string }[];
  absent?: string[];
}

/** Category spending of Seeds.OneCard in December 2025, before and after the market stall row is recategorized. */
export class OneCardCategorySpending {
  static readonly December2025: CategorySpending = {
    total: '$659.74',
    lines: [
      { name: 'Groceries', amount: '$86.40' },
      { name: 'Other', amount: '$31.00' },
    ],
  };

  /** The December market stall row, $31.00, moved from Other to Groceries. */
  static readonly December2025MarketStallInGroceries: CategorySpending = {
    total: '$659.74',
    lines: [{ name: 'Groceries', amount: '$117.40' }],
    absent: ['Other'],
  };
}

/** Reports for Seeds.ThreeCards: 62 transactions in Dec 2025 and Jan 2026, six needing review. */
export class DashboardReports {
  static readonly AllTime: DashboardReport = {
    window: 'Dec 1, 2025 - Jan 27, 2026',
    totalSpend: '$2,947.08',
    transactionCount: 62,
    categories: { total: '$2,947.08', largest: { name: 'Travel', amount: '$783.94' } },
    topMerchant: { name: 'DELTA AIR LINES', amount: '$783.94', transactionCount: '2 transactions' },
    biggest: { label: 'ALL TIME', first: { merchant: 'DELTA AIR LINES 0062', amount: '$412.60' } },
    needsAttention: 6,
  };

  static readonly December2025: DashboardReport = {
    window: 'Dec 1 - Dec 31, 2025',
    totalSpend: '$1,551.09',
    transactionCount: 31,
    categories: { total: '$1,551.09', largest: { name: 'Travel', amount: '$412.60' } },
    topMerchant: { name: 'DELTA AIR LINES', amount: '$412.60', transactionCount: '1 transaction' },
    biggest: { label: '2025-12', first: { merchant: 'DELTA AIR LINES 0062', amount: '$412.60' } },
    needsAttention: 6,
  };

  static readonly Year2026: DashboardReport = {
    window: 'Jan 1 - Dec 31, 2026',
    totalSpend: '$1,395.99',
    transactionCount: 31,
    categories: { total: '$1,395.99', largest: { name: 'Travel', amount: '$371.34' } },
    topMerchant: { name: 'DELTA AIR LINES', amount: '$371.34', transactionCount: '1 transaction' },
    biggest: { label: '2026', first: { merchant: 'DELTA AIR LINES 0062', amount: '$371.34' } },
    needsAttention: 6,
  };

  /** A window before any data: nothing is spent, but Needs Attention still counts every window. */
  static readonly March2025: DashboardReport = {
    window: 'Mar 1 - Mar 31, 2025',
    totalSpend: '$0.00',
    transactionCount: 0,
    biggest: { label: '2025-03' },
    needsAttention: 6,
  };
}
