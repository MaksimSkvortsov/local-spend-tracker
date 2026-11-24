/** A year and month chosen in the dashboard's period selects. */
export interface DashboardPeriod {
  year: string;
  month: string;
}

export class DashboardPeriods {
  static readonly December2025: DashboardPeriod = { year: '2025', month: 'December' };

  static readonly March2025: DashboardPeriod = { year: '2025', month: 'March' };

  /** The year chosen in Year mode. */
  static readonly Year2026 = '2026';

  /** Only years that have transactions are listed, newest first. */
  static readonly YearOptions = ['2026', '2025'];

  static readonly MonthOptions = [
    'January',
    'February',
    'March',
    'April',
    'May',
    'June',
    'July',
    'August',
    'September',
    'October',
    'November',
    'December',
  ];
}
