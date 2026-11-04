# Import Dates

## Application Overview

Cards shows when each card was last imported, and Import History shows when each statement was imported. Both are stored in UTC and must be shown as the same local calendar date.

Environment: tests run against the UI harness (http://localhost:5246) with SPENDNEST_UiHarness__Ai=Fake and SPENDNEST_UiHarness__TimeZone=America/New_York. Seeded imports finish at 02:00 UTC on the 2nd of the month after their statement, which is 21:00 on the 1st in New York, so a date shown in UTC would read one day late.

## Test Scenarios

### 1. Import dates

**Seed:** `tests/seed.spec.ts`

#### 1.1. User sees import dates in local time on Cards and Import History

**File:** `tests/import-dates/user-sees-local-import-dates.spec.ts`

**Steps:**
  1. Seed Seeds.OneCard and open Cards
    - expect: Cards lists only "Chase Sapphire" with 20 transactions, 2 imports, and status "Active"
    - expect: Last Imported is "Feb 1, 2026", the local date of the January import, not "Feb 2, 2026"
  2. Open Import from the navigation
    - expect: Import History lists "sapphire-2025-12.csv" with Date Imported "Jan 1, 2026"
    - expect: Import History lists "sapphire-2026-01.csv" with Date Imported "Feb 1, 2026", matching Cards
