# Card Management

## Application Overview

The Cards page (sidebar "Cards", /cards) lists every card with its transaction count, import count, last imported date, and status, sorted by name, with Rename, Combine, and Delete actions per row. A summary chip shows the total card and transaction counts, and a "Search cards by name..." box filters the table (case-insensitive) with a "Showing N cards" count. The acceptance criteria are in docs/features/card-management/design.md.

- Rename edits the name inline (a textbox plus Save and Cancel in the row). The name is trimmed. Blank or whitespace-only names and names already used by another card (compared case-insensitively) are rejected. Only one row edits at a time: clicking Rename on another row moves the editor there.
- Status is "Active" with transactions, "Empty" without, and "Duplicate suspected" on every card whose name matches another once " Backup" is removed and case is ignored (for example "Chase Sapphire" and "Chase Sapphire Backup").
- Combine opens a "Combine Cards" dialog with labelled "Source Card" and "Target Card" selects and an impact summary. Choosing the same card for source and target resets the target to the first other card, so a card cannot be combined into itself. "Combine Cards" then asks for confirmation in a "Combine cards?" dialog; cancelling it returns to the Combine Cards dialog. Combine moves the source card's transactions and imports to the target without deduplicating.
- Delete asks for confirmation in a "Delete "<name>"?" dialog and removes the card's transactions and import history, but keeps shared data such as category rules and settings.
- Success and error messages appear above the table as "<message>" or "Could not update cards: <reason>". Starting a rename clears the previous message.
- Expected behavior the app does not have yet. The tests assert it and fail until it is fixed; each affected step is marked "Note: fails on the current app":
  - Cancelling a rename clears a validation error. Today the error stays after Cancel.
  - After a failed rename, combine, or delete, the page reloads the card list so it shows the real current cards, as the design doc's "keeps the user oriented to the current card list" requires. Today the stale list stays until a page reload.
  - When the reloaded list no longer has the card, its rename editor, combine dialogs, or delete dialog close and the error shows above the table. Today the delete dialog stays open and its error is hidden behind the modal backdrop, because DeleteCardConfirmation has no error area.

Environment: the UI harness at http://localhost:5246 with fake AI. Seeds.ThreeCards gives Chase Sapphire (20 transactions), CapOne Debit (20), and BofA Checking (22), each with 2 imports (Dec 2025 and Jan 2026) last imported Feb 1, 2026, plus a seeded "UBER" rule (Transportation), so the page shows 3 cards and 62 transactions. Seeds.OneCard gives only Chase Sapphire and the same rule. Seed ids are fixed, so calling HarnessHelper.seed(Seeds.OneCard) while the page still shows ThreeCards removes CapOne Debit and BofA Checking behind the page's back, the way the console app sharing the database would. Use Cards from test-data/cards.ts for names; do not assert money totals.

Locator notes: the rename textbox has no accessible name, and while editing, the row's name is the textbox value, so a row filtered by name text no longer matches. Reach the editor as the only textbox in the cards table. Status and validation messages have no status role.

Not covered: the "Could not load cards" state needs a way to make loading fail, which the harness does not offer.

## Test Scenarios

### 1. Card management

**Seed:** `tests/seed.spec.ts`

#### 1.1. User renames a card and sees the new name everywhere

**File:** `tests/card-management/user-renames-a-card.spec.ts`

**Steps:**
  1. Seed Seeds.ThreeCards and open Cards from the navigation
    - expect: Cards shows "3 cards" and "62 transactions"
    - expect: The table lists BofA Checking, CapOne Debit, and Chase Sapphire, in that order, each Active with 2 imports
  2. Click "Rename" on the Chase Sapphire row, change the name to "Draft", then click "Rename" on the BofA Checking row
    - expect: While editing, the row shows a textbox holding "Chase Sapphire" with Save and Cancel instead of Rename, Combine, and Delete
    - expect: After the second Rename, only one textbox is open and it holds "BofA Checking"; the Chase Sapphire row shows "Chase Sapphire" again, so the draft was discarded
  3. Click "Cancel"
    - expect: Every row shows its original name with Rename, Combine, and Delete
  4. Click "Rename" on Chase Sapphire, enter only spaces, and click "Save"
    - expect: The message "Could not update cards: Card name is required." is shown
    - expect: The textbox stays open and the card count is still 3
  5. Enter "capone debit" (another card's name in different case) and click "Save"
    - expect: The message "Could not update cards: Another card already uses that name." is shown
    - expect: The textbox stays open
  6. Click "Cancel"
    - expect: The row shows "Chase Sapphire" with Rename, Combine, and Delete
  7. Click "Rename" on Chase Sapphire, enter "  Sapphire Preferred  " with surrounding spaces, and click "Save"
    - expect: The message "Card renamed." is shown
    - expect: The table lists BofA Checking, CapOne Debit, and "Sapphire Preferred" (trimmed); Sapphire Preferred keeps 20 transactions, 2 imports, and status Active
    - expect: No row shows "Chase Sapphire"
  8. Open Transactions from the navigation and open the "Card" filter
    - expect: The Card filter options are "All cards", "BofA Checking", "CapOne Debit", and "Sapphire Preferred"
    - expect: Transaction rows show "Sapphire Preferred" and none show "Chase Sapphire"
  9. Open Dashboard from the navigation, then open Cards and reload the page
    - expect: The dashboard shows "Sapphire Preferred" and not "Chase Sapphire"
    - expect: After reload, Cards still lists "Sapphire Preferred"
  10. Click "Rename" on Sapphire Preferred, enter "capone debit", click "Save", then click "Cancel"
    - expect: After Save, the message "Could not update cards: Another card already uses that name." is shown
    - expect: After Cancel, the error message is gone and the row shows "Sapphire Preferred" with Rename, Combine, and Delete
    - Note: fails on the current app. "Another card already uses that name." stays after Cancel because CardsTable.CancelRename does not clear the page status. Fix: clear the status on cancel (for example an OnCancelRename callback to Cards.razor's ClearStatus).

#### 1.2. User searches cards and renames a card out of the filter

**File:** `tests/card-management/user-searches-cards.spec.ts`

**Steps:**
  1. Seed Seeds.ThreeCards, open Cards, and type "SAPPHIRE" in "Search cards by name..."
    - expect: The table shows only Chase Sapphire and the count reads "Showing 1 card"
    - expect: The summary chip still shows "3 cards" and "62 transactions"
  2. Replace the search with "amex"
    - expect: The table reads "No cards found." and the count reads "Showing 0 cards"
  3. Replace the search with "capone", click "Rename" on CapOne Debit, enter "Everyday Debit", and click "Save"
    - expect: The message "Card renamed." is shown
    - expect: The renamed card no longer matches the search, so the table reads "No cards found." with "Showing 0 cards"
    - expect: Focus moves to the search box
  4. Clear the search
    - expect: The count reads "Showing 3 cards" and the table lists BofA Checking, Chase Sapphire, and Everyday Debit

#### 1.3. User combines a suspected duplicate card

**File:** `tests/card-management/user-combines-cards.spec.ts`

**Steps:**
  1. Seed Seeds.ThreeCards, open Cards, and rename CapOne Debit to "Chase Sapphire Backup"
    - expect: The message "Card renamed." is shown
    - expect: Chase Sapphire and Chase Sapphire Backup both show the status "Duplicate suspected"; BofA Checking stays Active
  2. Click "Combine" on the Chase Sapphire Backup row
    - expect: The "Combine Cards" dialog opens with Source Card "Chase Sapphire Backup (20 transactions)" and Target Card "BofA Checking (22 transactions)"
    - expect: The summary reads "20 transactions from Chase Sapphire Backup will move to BofA Checking. Transactions are not deduplicated."
  3. Choose "BofA Checking (22 transactions)" as the Source Card
    - expect: The Target Card is reset to "Chase Sapphire (20 transactions)", the first other card, so source and target never match
    - expect: The summary reads "22 transactions from BofA Checking will move to Chase Sapphire."
  4. Click "Cancel" in the Combine Cards dialog
    - expect: The dialog closes and Cards still shows "3 cards" and "62 transactions" with both Duplicate suspected rows
  5. Click "Combine" on Chase Sapphire Backup again, choose "Chase Sapphire Backup (20 transactions)" as the Target Card
    - expect: The Target Card is reset to a different card, so a card cannot be combined into itself
  6. Choose "Chase Sapphire (20 transactions)" as the Target Card and click "Combine Cards"
    - expect: The summary reads "20 transactions from Chase Sapphire Backup will move to Chase Sapphire."
    - expect: The "Combine cards?" dialog asks "Combine Chase Sapphire Backup into Chase Sapphire? Transactions are not deduplicated." with Cancel and Combine Cards
  7. Click "Cancel" in the "Combine cards?" dialog
    - expect: The confirmation closes and the "Combine Cards" dialog is shown again with the same source and target
    - expect: Behind it, Cards still shows "3 cards"
  8. Click "Combine Cards" again and confirm with "Combine Cards" in the "Combine cards?" dialog
    - expect: Both dialogs close and the message "Chase Sapphire Backup combined into Chase Sapphire." is shown
    - expect: Cards shows "2 cards" and still "62 transactions"
    - expect: Chase Sapphire shows 40 transactions, 4 imports, and status Active; no row is Duplicate suspected and Chase Sapphire Backup is no longer listed
  9. Open Transactions from the navigation, then open Import from the navigation
    - expect: Transactions shows "62 Transactions" and the Card filter lists only "All cards", "BofA Checking", and "Chase Sapphire"
    - expect: Import History still lists capone-2025-12.csv and capone-2026-01.csv with 10 transactions each and status COMPLETED

#### 1.4. User deletes a card and its data

**File:** `tests/card-management/user-deletes-a-card.spec.ts`

**Steps:**
  1. Seed Seeds.ThreeCards, open Cards, and click "Delete" on the CapOne Debit row
    - expect: The dialog "Delete "CapOne Debit"?" asks "Are you sure you want to delete this card and its associated transaction history?" with Cancel and Delete Card
  2. Click "Cancel"
    - expect: The dialog closes and Cards still shows "3 cards", "62 transactions", and the CapOne Debit row
  3. Click "Delete" on CapOne Debit again and click "Delete Card"
    - expect: The message "CapOne Debit deleted." is shown
    - expect: Cards shows "2 cards" and "42 transactions", listing BofA Checking and Chase Sapphire only
  4. Open Transactions, then Import, from the navigation
    - expect: Transactions shows "42 Transactions" and the Card filter lists only "All cards", "BofA Checking", and "Chase Sapphire"
    - expect: Import History lists the four boa and sapphire statements and no capone statement
  5. Open Dashboard from the navigation, click "All", then open Rules and Settings from the navigation
    - expect: The dashboard reports 42 transactions and no longer shows "CapOne Debit"
    - expect: Rules still shows the "UBER" rule for Transportation
    - expect: Settings still shows the AI Configuration status "Connected"

#### 1.5. User deletes their only card and onboarding returns

**File:** `tests/card-management/user-deletes-their-only-card.spec.ts`

**Steps:**
  1. Seed Seeds.OneCard and open Cards from the navigation
    - expect: Cards shows "1 card" and "20 transactions" with Chase Sapphire
    - expect: "Combine" is disabled on the Chase Sapphire row because there is no other card
  2. Click "Delete" on Chase Sapphire and confirm with "Delete Card"
    - expect: The message "Chase Sapphire deleted." is shown
    - expect: Cards shows "0 cards", "0 transactions", and "No cards imported yet."
  3. Open Dashboard from the navigation
    - expect: The "Welcome to PeasantMoney" panel is shown with "Configure AI in Settings" checked and the "Import Transactions" call-to-action
  4. Open Rules from the navigation
    - expect: Rules still lists the "UBER" rule for Transportation with 0 matches

#### 1.6. Card changes fail when the cards were changed elsewhere

**File:** `tests/card-management/card-changes-fail-for-removed-cards.spec.ts`

**Steps:**
  1. Seed Seeds.ThreeCards and open Cards from the navigation, then seed Seeds.OneCard through the harness without reloading, so CapOne Debit and BofA Checking no longer exist
    - expect: The page still shows the stale list: "3 cards" with BofA Checking, CapOne Debit, and Chase Sapphire
  2. Click "Rename" on CapOne Debit, enter "Everyday Debit", and click "Save"
    - expect: The message "Could not update cards: Card was not found." is shown
    - expect: The list is reloaded: Cards shows "1 card" and "20 transactions" with only Chase Sapphire, and no rename textbox is open
    - Note: fails on the current app. The error is shown, but the list keeps all 3 cards and the textbox stays open, because RunCardOperationAsync in Cards.razor reloads only on success. Fix: reload the list in the failure path too, and close the editor when its card is gone.
  3. Seed Seeds.ThreeCards, reload the page, and seed Seeds.OneCard through the harness again
    - expect: The page shows the stale list of 3 cards again
  4. Click "Combine" on CapOne Debit, choose "Chase Sapphire (20 transactions)" as the Target Card, click "Combine Cards", and confirm with "Combine Cards"
    - expect: Both combine dialogs close and the message "Could not update cards: Card was not found." is shown above the table
    - expect: The list is reloaded: Cards shows "1 card" and "20 transactions" with only Chase Sapphire
    - Note: fails on the current app. The "Combine cards?" dialog stays open with the error inside it, and the list keeps all 3 cards. Same fix as step 2; once the list is reloaded, the confirmation's source card is gone, so the workflow should close.
  5. Seed Seeds.ThreeCards, reload the page, and seed Seeds.OneCard through the harness again
    - expect: The page shows the stale list of 3 cards again
  6. Click "Delete" on BofA Checking and click "Delete Card"
    - expect: The dialog "Delete "BofA Checking"?" closes and the message "Could not update cards: Card was not found." is visible above the table
    - expect: The list is reloaded: Cards shows "1 card" and "20 transactions" with only Chase Sapphire
    - Note: fails on the current app. The delete dialog stays open with no error inside it (DeleteCardConfirmation has no error area), the error sits behind the modal backdrop, and the list keeps all 3 cards. Same fix as step 2; the dialog closes once the reloaded list has no pending card. A delete that fails while the card still exists would still hide its error, so DeleteCardConfirmation should also get an ErrorMessage like CombineCardsConfirmation.
  7. Reload the page
    - expect: Cards still shows "1 card" with only Chase Sapphire, matching what the page showed after the failure
