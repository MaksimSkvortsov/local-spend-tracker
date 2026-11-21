import { Cards } from './cards';
import { Categories } from './categories';

/** One row of the Transactions table, as the Transactions page shows it. */
export interface TransactionRow {
  /** As shown, for example "Dec 23, 2025". */
  date: string;
  description: string;
  card: string;
  category: string;
  /** Shows "Low Confidence" and a Confirm button. */
  needsReview: boolean;
  /** As shown, for example "+ $2,800.00" for money in; not checked when left out. */
  amount?: string;
}

/** Rows of Seeds.ThreeCards. Amounts are left out because they are not asserted. */
export class TransactionRows {
  static readonly MarketStallDecember: TransactionRow = {
    date: 'Dec 23, 2025',
    description: 'SQ *MARKET STALL 22',
    card: Cards.ChaseSapphire.name,
    category: Categories.Other,
    needsReview: true,
  };

  static readonly MarketStallJanuary: TransactionRow = {
    ...TransactionRows.MarketStallDecember,
    date: 'Jan 23, 2026',
  };

  static readonly VenmoDecember: TransactionRow = {
    date: 'Dec 24, 2025',
    description: 'VENMO *J SMITH',
    card: Cards.CapOneDebit.name,
    category: Categories.Other,
    needsReview: true,
  };

  static readonly VenmoJanuary: TransactionRow = { ...TransactionRows.VenmoDecember, date: 'Jan 24, 2026' };

  static readonly ZelleDecember: TransactionRow = {
    date: 'Dec 25, 2025',
    description: 'ZELLE PAYMENT TO R LEE',
    card: Cards.BofaChecking.name,
    category: Categories.Other,
    needsReview: true,
  };

  static readonly ZelleJanuary: TransactionRow = { ...TransactionRows.ZelleDecember, date: 'Jan 25, 2026' };

  static readonly TargetRefundDecember: TransactionRow = {
    date: 'Dec 21, 2025',
    description: 'TARGET REFUND',
    card: Cards.CapOneDebit.name,
    category: Categories.Shopping,
    needsReview: false,
  };

  static readonly TargetRefundJanuary: TransactionRow = { ...TransactionRows.TargetRefundDecember, date: 'Jan 21, 2026' };

  /** The first BofA Checking row, money in. */
  static readonly DirectDepositDecember: TransactionRow = {
    date: 'Dec 1, 2025',
    description: 'DIRECT DEP EMPLOYER INC',
    card: Cards.BofaChecking.name,
    category: Categories.IncomeAndTransfers,
    needsReview: false,
    amount: '+ $2,800.00',
  };

  static readonly DeltaDecember: TransactionRow = {
    date: 'Dec 14, 2025',
    description: 'DELTA AIR LINES 0062',
    card: Cards.ChaseSapphire.name,
    category: Categories.Travel,
    needsReview: false,
  };

  static readonly DeltaJanuary: TransactionRow = { ...TransactionRows.DeltaDecember, date: 'Jan 14, 2026' };

  /** The December market stall row after Confirm: it keeps its category and loses the review mark. */
  static readonly MarketStallDecemberConfirmed: TransactionRow = {
    ...TransactionRows.MarketStallDecember,
    needsReview: false,
  };

  /** The December Venmo row after choosing Shopping. */
  static readonly VenmoDecemberShopping: TransactionRow = {
    ...TransactionRows.VenmoDecember,
    category: Categories.Shopping,
    needsReview: false,
  };
}
