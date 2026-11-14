# Transaction Review

## Application Overview

The Transactions page (sidebar "Transactions", /transactions) lists every imported transaction, 10 per page, with Date, Description, Category, Card, and Amount. Money in is shown as "+ $X". The toolbar has a "Review filter" group with "All" and "Require Review" (the button shows the number of transactions needing review), a "<N> Transactions" count chip, and "Category" and "Card" filter selects. The footer reads "Showing <shown> of <total> transactions" with "Previous page" and "Next page" buttons and no page label. Changing any filter returns to page 1.

- Every row has a category select labelled "Category for <description>". Choosing a different category saves it right away and marks the transaction reviewed.
- Rows that need review show "Low Confidence" and a "Confirm" button, which keeps the current category and marks the transaction reviewed.
- A failed change shows "Could not update category: <reason>" or "Could not confirm category: <reason>" above the table.
- Expected behavior the app does not have yet. The tests assert it and fail until it is fixed; each affected step is marked "Note: fails on the current app":
  - After a failed change or confirm, the page reloads the transactions so it shows the real current data, and the category select shows the saved category. Today the stale list stays, and the select keeps the category that was not saved.

Environment: the UI harness at http://localhost:5246 with fake AI. Seeds.ThreeCards gives 62 transactions over Dec 2025 and Jan 2026 for Chase Sapphire (20), CapOne Debit (20), and BofA Checking (22). Six need review, all categorized Other: "SQ *MARKET STALL 22" (Chase Sapphire, Dec 23, 2025 and Jan 23, 2026), "VENMO *J SMITH" (CapOne Debit, Dec 24, 2025 and Jan 24, 2026), and "ZELLE PAYMENT TO R LEE" (BofA Checking, Dec 25, 2025 and Jan 25, 2026). Seed ids are fixed, so calling HarnessHelper.seed(Seeds.OneCard) while the page still shows ThreeCards removes the CapOne Debit and BofA Checking transactions behind the page's back, the way the console app sharing the database would. Do not assert money totals other than the per-row amounts named in the steps.

Locator notes: each description appears once per month, so the category select label "Category for <description>" is not unique. Find a row by date and description, then use its select and Confirm button. The count chip and footer have no role or label.

## Test Scenarios

### 1. Transaction review

**Seed:** `tests/seed.spec.ts`

#### 1.1. User reviews low-confidence transactions

**File:** `tests/transaction-review/user-reviews-low-confidence-transactions.spec.ts`

**Steps:**
  1. Seed Seeds.ThreeCards and open Transactions from the navigation
    - expect: The count chip shows "62 Transactions" and "Require Review" shows 6
    - expect: The footer reads "Showing 10 of 62 transactions" and "Previous page" is disabled
  2. Click "Require Review"
    - expect: The count chip shows "6 Transactions" and the footer reads "Showing 6 of 6 transactions"
    - expect: The six review rows are listed, each with category Other, "Low Confidence", and a "Confirm" button
  3. Click "Confirm" on the Dec 23, 2025 "SQ *MARKET STALL 22" row
    - expect: The row leaves the list, the count chip shows "5 Transactions", and "Require Review" shows 5
  4. Choose "Shopping" for the Dec 24, 2025 "VENMO *J SMITH" row
    - expect: The row leaves the list, the count chip shows "4 Transactions", and "Require Review" shows 4
  5. Click "All", choose Category "Shopping" and Card "CapOne Debit"
    - expect: The count chip shows "3 Transactions": the two "TARGET REFUND" rows and the Dec 24, 2025 "VENMO *J SMITH" row
    - expect: The VENMO row shows category Shopping with no "Low Confidence" and no "Confirm"
  6. Choose Category "Other" and Card "Chase Sapphire"
    - expect: The count chip shows "2 Transactions": both "SQ *MARKET STALL 22" rows
    - expect: The confirmed Dec 23, 2025 row keeps category Other without "Low Confidence" or "Confirm"; the Jan 23, 2026 row still shows "Low Confidence" and "Confirm"
  7. Reload the page
    - expect: "Require Review" shows 4 and the filters are back to All, "All Categories", and "All cards" with "62 Transactions"

#### 1.2. User filters and pages through transactions

**File:** `tests/transaction-review/user-filters-transactions.spec.ts`

**Steps:**
  1. Seed Seeds.ThreeCards, open Transactions, and click "Next page" six times
    - expect: The footer reads "Showing 2 of 62 transactions" and "Next page" is disabled
  2. Click "Previous page"
    - expect: The footer reads "Showing 10 of 62 transactions" and both page buttons are enabled
  3. Choose Card "BofA Checking"
    - expect: The view returns to page 1: the footer reads "Showing 10 of 22 transactions", "Previous page" is disabled, and the first row is Dec 1, 2025 "DIRECT DEP EMPLOYER INC"
    - expect: Money in is shown with a plus sign: "DIRECT DEP EMPLOYER INC" shows "+ $2,800.00"
  4. Choose Category "Travel"
    - expect: The count chip shows "0 Transactions", the table reads "No transactions match these filters.", and no footer is shown
  5. Choose Card "All cards"
    - expect: The count chip shows "2 Transactions": the two Chase Sapphire "DELTA AIR LINES 0062" rows, Dec 14, 2025 and Jan 14, 2026
  6. Click "Require Review"
    - expect: The count chip shows "0 Transactions" and the table reads "No transactions match these filters.", because no Travel transaction needs review

#### 1.3. Category changes fail for transactions removed elsewhere

**File:** `tests/transaction-review/category-changes-fail-for-removed-transactions.spec.ts`

**Steps:**
  1. Seed Seeds.ThreeCards, open Transactions, and choose Card "BofA Checking", then seed Seeds.OneCard through the harness without reloading
    - expect: The page still shows the stale list: "22 Transactions" for BofA Checking
  2. Choose "Other Card Payment" for the Dec 25, 2025 "ZELLE PAYMENT TO R LEE" row
    - expect: An error matching "Could not update category:" and "was not found." is shown above the table
    - expect: The page reloads the transactions: with Card "All cards" the count chip shows "20 Transactions", and no BofA Checking row is listed
    - Note: fails on the current app. The error is shown, but the list keeps the stale BofA Checking rows and the select keeps "Other Card Payment", which was not saved, because ChangeCategoryAsync in Transactions.razor reloads only on success. Fix: reload the page data in the failure path too.
  3. Seed Seeds.ThreeCards, reload the page, seed Seeds.OneCard through the harness again, and click "Require Review"
    - expect: The page still shows the stale "6 Transactions" needing review
  4. Click "Confirm" on the Dec 25, 2025 "ZELLE PAYMENT TO R LEE" row
    - expect: An error matching "Could not confirm category:" and "was not found." is shown above the table
    - expect: The page reloads the transactions: "Require Review" shows 2 and only the two "SQ *MARKET STALL 22" rows are listed
    - Note: fails on the current app. The error is shown, but the six stale review rows stay. Same fix as step 2, in ConfirmCategoryAsync.
  5. Reload the page
    - expect: The count chip shows "20 Transactions" and "Require Review" shows 2, matching what the page showed after the failure
