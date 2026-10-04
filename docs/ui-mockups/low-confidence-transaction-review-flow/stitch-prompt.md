# Stitch Prompt: Low-Confidence Transaction Review Flow

Use project `14634745227908556953` / `https://stitch.withgoogle.com/projects/14634745227908556953?pli=1`.

Use screenshots already present in the Stitch project as visual references for Spendnest/PeasantMoney layout, density, navigation, styling, and component treatment. Match the existing desktop-first finance app style: left sidebar, quiet light workspace, white bordered panels, compact toolbar controls, dense table layout, green-led spending visuals, and amber warning accents for review work.

Source requirements: `docs/features/low-confidence-transaction-review-flow/requirements.md`.

Generate a desktop web app mockup for the Transactions page Needs review mode.

## Feature Summary

When AI categorization marks imported transactions as low confidence, Spendnest should give users a focused way to protect category totals and spending reports from uncertain assignments. Users should be able to review each uncertain transaction, confirm the suggested category when it looks right, or choose a better built-in category from the available options.

## UX

- Primary flow: User opens Transactions, selects the Needs review filter or follows the dashboard Needs Attention entry point, and resolves low-confidence rows one at a time.
- Placement: First slice lives in the existing Transactions page/table; a separate Review page is not required.
- States: no imported transactions, no low-confidence transactions, loading, save error, per-row saving, resolved success, stale row already resolved, and no usable suggested category.
- Controls: Needs review filter, suggested category label, plain-language uncertainty reason, confidence as secondary metadata, Confirm category action, and category selector using built-in categories.

## UI

- Screen changes: Update the Transactions page Needs review state, the transaction table category cell, and the dashboard Needs Attention entry point when routing support exists.
- Layout: Keep the existing dense Transactions table. In review mode, category cells should expand enough to show a suggested-category label, uncertainty reason, and row actions without turning the table into cards.
- Components: Add or refine a warning-tinted suggested category pill, confidence text, short reason text, Confirm button, built-in category selector, per-row saving state, and inline row/page error message.
- Visual treatment: Treat unresolved rows as review work with amber/warning accents, keep money/date/card columns scannable, use concise copy, and ensure row actions remain keyboard reachable in table order.
- Empty states: Reuse the existing no-transactions empty state when no data exists; in the Needs review filter, show a distinct "All low-confidence transactions reviewed" state.

## Required Mockup Content

- Show the Transactions page with the Needs review filter active.
- Include realistic finance rows sorted by lowest confidence first, then newest posted date.
- Each row should show date, description, card, amount, suggested category, short uncertainty reason, and confidence as secondary metadata.
- Show at least one row with a suggested category and a Confirm action.
- Show at least one row where the user can choose another built-in category.
- Show at least one row with no usable suggested category where Confirm is absent and category selection is primary.
- Include a compact inline error or stale-row message state.
- Include a clear empty state treatment for "All low-confidence transactions reviewed" if there are no rows.

Avoid a marketing page, oversized hero layout, nested cards, decorative gradients, custom categories, bulk actions, saved-rule UI, AI retry controls, or a separate Review page.
