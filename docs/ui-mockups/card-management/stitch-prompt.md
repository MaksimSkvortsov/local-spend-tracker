# Stitch Prompt: Card Management

Source feature document: `docs/features/card-management/design.md`

Project: PeasantMoney
Project ID: `14634745227908556953`
Project URL: `https://stitch.withgoogle.com/projects/14634745227908556953?pli=1`

Use screenshots already present in the Stitch project as visual references for layout, density, navigation, styling, table treatment, settings/form controls, and component treatment. Especially reference the existing Dashboard, Import, Transactions, Settings, and Rules screens.

## Request

Generate a desktop-first PeasantMoney / Spendnest screen for a new top-level sidebar destination named **Cards**.

This is a local-first finance app for credit-card spending. Build the actual feature screen, not a marketing page.

## Feature Summary

Users can accidentally create separate card records for the same real credit card during imports, or keep cards they no longer want in the local database. Add a Cards page so users can rename cards, combine duplicate cards, and delete a card with its imported data while clearly understanding what will change.

## UX

- Primary flow: User opens Cards from the sidebar, reviews cards, renames a card inline, combines one card into another, or deletes a card after confirmation.
- Placement: Add Cards as a top-level sidebar destination alongside Dashboard, Import, Transactions, Rules, and Settings.
- States to represent in the main generated screen: loaded card list, inline rename affordance, combine controls, and a delete confirmation/dialog state.
- Other states to account for in visible design language: empty, load failed, validation error, in progress, success, and error.
- Controls: Rename text input/action per card, source/target selectors or equivalent combine controls, Delete per card, and confirmation dialogs for combine/delete.

## UI

- Screen changes: Add a top-level Cards page for card management.
- Layout: Show cards in a table/list with card name, transaction count, import counts, rename action, and delete action; place combine controls below or beside the list in a compact management area.
- Components: Text inputs, compact buttons, searchable/selectable source/target controls, warning copy, confirmation dialog, impact summary, and success/error status message treatment.
- Visual treatment: Match the existing quiet desktop utility style: left sidebar, light workspace, white panels, compact controls, dense readable tables, green primary actions, red destructive actions, and restrained amber/red warning treatment.

## Acceptance Criteria Affecting UI

- A user can open the Cards page from the sidebar.
- The Cards page shows the user's cards and enough summary information to distinguish them.
- The Cards page distinguishes between no cards and a load failure.
- A user can rename a card and see the new name reflected anywhere card names appear.
- Rename preserves existing transactions and import history.
- Rename validation prevents blank or duplicate card names.
- A user can combine one card into another after a confirmation that includes both card names.
- After combine, the source card no longer appears and its transactions/import history belong to the target card.
- Combining a card into itself is blocked.
- Combining cards does not deduplicate transactions.
- A user can delete a card only after a simple confirmation that includes the card name.
- After delete, the card and its card-owned data no longer appear in card-dependent views.
- Deleting a card does not delete shared app data such as categories, category rules, or settings.
- Canceling combine/delete confirmation leaves data unchanged.
- If an operation fails, the UI shows an error and keeps the user oriented to the current card list.

## Realistic Data

Use card rows like:

- Family Visa, 842 transactions, 18 imports, last import Sep 9, 2026
- Travel Amex, 126 transactions, 4 imports, last import Aug 29, 2026
- Costco Citi, 215 transactions, 7 imports, last import Sep 2, 2026
- Family Visa Backup, 37 transactions, 1 import, possible duplicate naming scenario

Use a combine scenario such as combining `Family Visa Backup` into `Family Visa`.

Use a delete confirmation state for `Travel Amex`.

## Constraints

- Desktop-first at 1440px-ish width.
- Keep the layout dense and scannable.
- Do not create a hero section, marketing copy, decorative gradients, or oversized typography.
- Do not make nested cards inside cards.
- Keep confirmation copy short and product-level.
- Use stable table/control dimensions so labels and button text fit.
- Include a clear active Cards nav item in the sidebar.
