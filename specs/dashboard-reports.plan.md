# Dashboard Reports

## Application Overview

Once transactions exist, the Dashboard (sidebar "Dashboard", /dashboard) shows reports for a chosen window. The "Report mode" group has All, Year, and Month buttons; Year adds a "Year" select listing only years with data, and Month adds a "Month" select listing all twelve months. The metric panel shows "Window" (for example "Dec 1 - Dec 31, 2025" or "Dec 1, 2025 - Jan 27, 2026" for All), "Total Spend", and "Transactions" (every transaction in the window, including payments and income). The cards below are "Spending by Category" (TOTAL, then categories by amount, or "No category spending in this window."), "Top Merchants" (top 5 with "<n> transaction(s)", or "No merchant spending in this window."), "Needs Attention", "Biggest Transactions" ("TOP 5 <window>", where the window is "ALL TIME", a year such as "2026", or a month such as "2025-12"), "Transaction Frequency", and "Amount Spent per Day".

- Spending counts purchases minus refunds and leaves out payments and income.
- "Needs Attention" counts every transaction needing review in all windows: "<n> transactions require attention" with "Low classification confidence detected for recent activity." and a "Review now" link to Transactions, or "All transactions look good" with "No low-confidence categorizations found." and no link.
- The chosen window is kept across navigation and reload; seeding resets it. On first load the dashboard opens on the current year, so tests must choose their window and must not assert the default.
- Category changes made on Transactions show on the dashboard without a reload.
- Expected behavior the app does not have yet. The tests assert it and fail until it is fixed; each affected step is marked "Note: fails on the current app":
  - One transaction needing review reads "1 transaction requires attention". Today it reads "1 transaction require attention".

Environment: the UI harness at http://localhost:5246 with fake AI. Categories come from the seed templates, not the fake categorizer, so money totals here follow product rules and can be asserted. Seeds.ThreeCards gives 62 transactions, 31 per month, in Dec 2025 and Jan 2026, six needing review. Seeds.OneCard gives Chase Sapphire only: Dec 2025 spending $659.74 (Travel $412.60, Groceries $86.40, Restaurants & Coffee $54.95, Shopping $35.00, Other $31.00, Transportation $24.30, Subscriptions $15.49), with "SQ *MARKET STALL 22" ($31.00, Dec 23, 2025 and $27.90, Jan 23, 2026) needing review. Spending totals also come back from HarnessHelper.seed().

Locator notes: the metric values have no labels (the existing TODO in dashboard.page.ts), and the report cards are found by their titles.

Not covered: "Biggest Transactions" shows only its header row when the window has no spending, with no empty message like the other cards; the expected copy is not specified anywhere, so this plan only checks that no rows are listed. "Transaction Frequency" and "Amount Spent per Day" are charts and are only checked for being present.

## Test Scenarios

### 1. Dashboard reports

**Seed:** `tests/seed.spec.ts`

#### 1.1. User explores spending by period

**File:** `tests/dashboard-reports/user-explores-spending-by-period.spec.ts`

**Steps:**
  1. Seed Seeds.ThreeCards, open Dashboard from the navigation, and click "All"
    - expect: The window is "Dec 1, 2025 - Jan 27, 2026", Total Spend is "$2,947.08", and Transactions is 62
    - expect: No Year or Month select is shown
    - expect: Spending by Category shows TOTAL "$2,947.08" with Travel "$783.94" first
    - expect: Top Merchants starts with "DELTA AIR LINES" "$783.94" "2 transactions"; Biggest Transactions reads "TOP 5 ALL TIME" and starts with "DELTA AIR LINES 0062" "$412.60"
    - expect: Needs Attention reads "6 transactions require attention" with a "Review now" link, and the Transaction Frequency and Amount Spent per Day cards are shown
  2. Click "Month", then choose Year "2025" and Month "December"
    - expect: The Year select lists only 2026 and 2025, and the Month select lists January to December
    - expect: The window is "Dec 1 - Dec 31, 2025", Total Spend is "$1,551.09", and Transactions is 31
    - expect: Top Merchants starts with "DELTA AIR LINES" "$412.60" "1 transaction", and Biggest Transactions reads "TOP 5 2025-12"
  3. Click "Year" and choose "2026"
    - expect: The window is "Jan 1 - Dec 31, 2026", Total Spend is "$1,395.99", Transactions is 31, and Biggest Transactions reads "TOP 5 2026"
  4. Click "Month", then choose Year "2025" and Month "March"
    - expect: The window is "Mar 1 - Mar 31, 2025", Total Spend is "$0.00", and Transactions is 0
    - expect: Spending by Category reads "No category spending in this window." and Top Merchants reads "No merchant spending in this window."
    - expect: Biggest Transactions lists no rows
    - expect: Needs Attention still reads "6 transactions require attention", because it covers all windows
  5. Open Cards from the navigation, open Dashboard again, then reload the page
    - expect: Both times the dashboard is still on Month, March 2025, with the window "Mar 1 - Mar 31, 2025"

#### 1.2. Dashboard reflects transaction review

**File:** `tests/dashboard-reports/dashboard-reflects-transaction-review.spec.ts`

**Steps:**
  1. Seed Seeds.OneCard, open Dashboard, click "Month", and choose Year "2025" and Month "December"
    - expect: Spending by Category shows TOTAL "$659.74" with Groceries "$86.40" and Other "$31.00"
    - expect: Needs Attention reads "2 transactions require attention" with a "Review now" link
  2. Click "Review now"
    - expect: The Transactions page opens at /transactions
  3. Click "Require Review" and choose "Groceries" for the Dec 23, 2025 "SQ *MARKET STALL 22" row, then open Dashboard from the navigation
    - expect: The dashboard is still on December 2025 with TOTAL "$659.74"; Groceries is "$117.40" and Other is no longer listed
  4. Click "Review now", click "Require Review", click "Confirm" on the Jan 23, 2026 "SQ *MARKET STALL 22" row, then open Dashboard from the navigation
    - expect: Needs Attention reads "All transactions look good" and "No low-confidence categorizations found." with no "Review now" link

#### 1.3. Needs Attention uses the singular for one transaction

**File:** `tests/dashboard-reports/needs-attention-uses-the-singular.spec.ts`

**Steps:**
  1. Seed Seeds.OneCard, open Transactions from the navigation, click "Require Review", and click "Confirm" on the Dec 23, 2025 "SQ *MARKET STALL 22" row
    - expect: Only the Jan 23, 2026 "SQ *MARKET STALL 22" row is left to review
  2. Open Dashboard from the navigation
    - expect: Needs Attention reads "1 transaction requires attention" with a "Review now" link
    - Note: fails on the current app. It reads "1 transaction require attention" because NeedsAttention.razor appends " require attention" to the count. Fix: use "requires" for one transaction.
