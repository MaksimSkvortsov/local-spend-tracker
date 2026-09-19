# Feature: Card Management

Date: 2026-09-13
Status: Proposed

## Summary

Users can accidentally create separate card records for the same real credit card during imports, or keep cards they no longer want in the local database. Add card management so users can rename cards, combine duplicate cards, and delete a card with its imported data while clearly understanding what will change.

## UX

- Primary flow: User opens Cards from the sidebar, reviews cards, renames a card inline, combines one card into another, or deletes a card after confirmation.
- Placement: Add Cards as a top-level sidebar destination alongside Dashboard, Import, Transactions, Rules, and Settings.
- States: Empty, loaded, load failed, editing name, validation error, in progress, combine confirmation, delete confirmation, success, and error.
- Controls: Rename text input and Save per card, searchable source/target selectors for combine, Delete per card, and confirmation dialogs for combine and delete.

## UI

- Screen changes: Add a top-level Cards page for card management.
- Layout: Show cards in a table/list with card name, transaction count, import counts, rename action, and delete action; reveal combine controls when a user chooses Combine.
- Components: Text inputs, compact buttons, searchable selectors, warning copy, confirmation dialogs, impact summaries, and success/error status messages.
- Visual treatment: Match existing settings density, white panels, compact controls, green primary actions, and red destructive actions. Destructive confirmations should be plain, specific, and avoid softening permanent data loss.

## Acceptance Criteria

- A user can open the Cards page from the sidebar.
- The Cards page shows the user's cards and enough summary information to distinguish them.
- The Cards page distinguishes between no cards and a load failure.
- A user can rename a card from the Cards page and see the new name anywhere card names appear without restarting the app.
- Rename preserves the card's existing transactions and import history.
- Rename validation prevents blank or duplicate card names.
- A user can combine one card into another card after a confirmation that includes both card names.
- After combine, the source card no longer appears and its transactions and import history belong to the target card.
- Combining a card into itself is blocked.
- Combining cards does not deduplicate transactions.
- A user can delete a card only after a simple confirmation that includes the card name.
- After delete, the card and its card-owned data no longer appear in cards, transactions, dashboards, reports, or import history.
- Deleting a card does not delete shared app data such as categories, category rules, or settings.
- Canceling a combine or delete confirmation leaves data unchanged.
- After rename, combine, or delete, visible card-dependent views reflect the change without requiring an app restart.
- If an operation fails, the UI shows an error and keeps the user oriented to the current card list.

## Out of Scope

- Transaction deduplication during combine.
- Undo or restore after deletion.
- Bulk card operations.
- Card metadata beyond name and counts.
- Creating cards outside the import workflow.
- Editing transaction-level card assignment directly.

## Open Questions

- Should the Transactions card filter include a shortcut from the Cards page?
