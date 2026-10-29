# Fresh Application Launch

## Application Overview

PeasantMoney (Spendnest) is a private, local personal-finance app: users configure OpenAI categorization in Settings, import bank/credit card CSV statements, and get spending reports on the Dashboard. On a fresh install (no AI settings, no data) the Dashboard shows a "Welcome to PeasantMoney" onboarding panel with a two-item Getting Started checklist ("Configure AI in Settings", "Upload the first statement in Import") and a single call-to-action that moves from "Configure AI" to "Import Transactions" as steps complete. Once the first statement is imported, the welcome panel is replaced by the reporting dashboard.

Environment: tests run against the UI harness (http://localhost:5246) with SPENDNEST_UiHarness__Ai=Fake, so the connection test and categorization never call OpenAI. Every test starts with `HarnessHelper.seed(Seeds.NewUser)`. The native file dialog is replaced by the harness: before clicking "browse files from your computer", call `HarnessHelper.selectStatementFile()` with the absolute path of the statement fixture.

Test data to add:
- `test-data/statements.ts` exporting `Statement` and `Statements.CapitalOneDecember` — fileName `capital-one.csv`, detected card `Capital One 2246`, 4 transactions. Copy `src/Spendnest.Application.Tests/Fixtures/Csv/capital-one.csv` into `e2e/tests/test-data/statements/` and resolve the absolute path from there.
- `test-data/ai-settings.ts` exporting `AiSettings` and `AiSettingsData.FakeKey` (`sk-test-fake-key`, default model `gpt-5.6-luna`, timeout 25).

Do not assert dashboard money totals or category breakdowns: the fake categorizer sends the fixture's merchants to review as Other, so those numbers reflect the fake, not product rules.

## Test Scenarios

### 1. Fresh launch

**Seed:** `tests/seed.spec.ts`

#### 1.1. New user completes onboarding from the dashboard

**File:** `tests/fresh-launch/new-user-completes-onboarding.spec.ts`

**Steps:**
  1. Seed Seeds.NewUser and open the app root "/"
    - expect: The app redirects to /dashboard and the page title is "Dashboard - PeasantMoney"
    - expect: The heading "Welcome to PeasantMoney" is visible with the text "Set up private AI categorization, then import your first credit card statement to unlock local spending reports."
    - expect: The "Getting started checklist" shows "Configure AI in Settings" and "Upload the first statement in Import", both unchecked (icon check_box_outline_blank)
    - expect: The only call-to-action is the "Configure AI" button; no reports (Total Spend, Top Merchants) are shown
    - expect: The primary navigation shows Dashboard, Import, Transactions, Rules, Cards, and Settings links
  2. Use the primary navigation to open Transactions, Cards, Rules, and Import in turn
    - expect: Transactions shows "0 Transactions" and "No transactions imported yet."
    - expect: Cards shows "0 cards", "0 transactions", and "No cards imported yet."
    - expect: Rules shows "0 rules" and "No category rules saved yet."
    - expect: Import shows the "Upload Statement" drop zone and Import History reads "No uploads in this session."
  3. Open Dashboard from the navigation and click "Configure AI"
    - expect: The Settings page opens at /settings with the heading "Settings"
    - expect: The AI Configuration section shows the status "Not configured" and provider "ChatGPT"
    - expect: The "OpenAI API Key *" field is empty with placeholder "sk-...", Model is "gpt-5.6-luna", and Request Timeout is 25
    - expect: "Delete Configuration" is disabled
  4. Enter AiSettingsData.FakeKey in "OpenAI API Key *" and click "Test Connection"
    - expect: The message "OpenAI connection succeeded." is shown
    - expect: The status still reads "Not configured" because nothing is saved yet
  5. Click "Save"
    - expect: The status changes to "Connected"
    - expect: The key help text changes to "Leave blank to keep the saved key, or enter a new one to replace it."
    - expect: "Delete Configuration" is enabled
  6. Open Dashboard from the navigation, then reload the page
    - expect: The welcome panel is still shown
    - expect: "Configure AI in Settings" is checked and "Upload the first statement in Import" is unchecked
    - expect: The call-to-action is now "Import Transactions" instead of "Configure AI"
    - expect: The same checklist state is shown after reload
  7. Click "Import Transactions"
    - expect: The Import page opens at /import with the heading "Import Transactions"
    - expect: Import History reads "No uploads in this session."
  8. Select Statements.CapitalOneDecember through the harness and click "browse files from your computer"
    - expect: "Transactions in File" shows "Previewing 4 transactions from capital-one.csv"
    - expect: The detected card is "Capital One 2246" with "Card detected from the selected CSV."
    - expect: The preview table lists 4 rows, and "Cancel" and "Confirm Import" buttons are visible
  9. Click "Confirm Import"
    - expect: The preview closes and the "Upload Statement" drop zone returns
    - expect: Import History lists capital-one.csv with count 4, range "Dec 31, 2025", and status COMPLETED
  10. Open Dashboard from the navigation
    - expect: The "Welcome to PeasantMoney" heading and Getting Started checklist are gone
    - expect: The reporting dashboard is shown with "Total Spend", "Transactions" showing 4, "Top Merchants", and "Needs Attention"
  11. Open Cards from the navigation
    - expect: Cards shows "1 card" and "4 transactions"
    - expect: The table lists "Capital One 2246" with 4 transactions, 1 import, and status Active

#### 1.2. New user imports before configuring AI

**File:** `tests/fresh-launch/new-user-imports-before-configuring-ai.spec.ts`

**Steps:**
  1. Seed Seeds.NewUser and open the app root "/"
    - expect: The Dashboard shows "Welcome to PeasantMoney" with both checklist items unchecked and the "Configure AI" call-to-action
  2. Open Import from the navigation, select Statements.CapitalOneDecember through the harness, and click "browse files from your computer"
    - expect: "Previewing 4 transactions from capital-one.csv" is shown with detected card "Capital One 2246"
    - expect: "Confirm Import" is enabled
  3. Click "Confirm Import"
    - expect: The error "Categorization failed for 4 of 4 transactions, so no transactions were saved. AI categorization failed: OpenAI API key is required for OpenAI categorization." is shown (match with a regular expression on "no transactions were saved" and "API key is required")
    - expect: Import History lists capital-one.csv with count 0, range "--", and status FAILED
  4. Open Cards, then Dashboard, from the navigation
    - expect: Cards shows "1 card" and "0 transactions": the failed import keeps "Capital One 2246" with 0 transactions, 1 import, and status Empty, so the import can be retried
    - expect: The Dashboard still shows the welcome panel with both checklist items unchecked and the "Configure AI" call-to-action
