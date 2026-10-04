# Stitch Prompt: Category Rules Management

Use project `14634745227908556953` / `https://stitch.withgoogle.com/projects/14634745227908556953?pli=1`.

Use screenshots already present in the Stitch project as visual references for Spendnest/PeasantMoney layout, density, navigation, styling, and component treatment. Match the existing desktop-first finance app style: fixed left sidebar, quiet light workspace, white bordered panels, compact toolbar controls, dense table layout, green primary actions, and amber warning copy for retroactive changes.

Source design doc: `docs/features/category-rules-management/design.md`.

Generate a desktop web app mockup for a new top-level Rules page.

## Feature Summary

Spendnest already creates category rules, but users cannot manage them. Add Rules as a top-level page where users can view saved rules, change the category a rule assigns, preview matching transactions, and apply the new category to those transactions.

Rules keep the current model: `Pattern`, `MatchType`, and `CategoryId`.

## User Flow

1. User opens Rules from the sidebar.
2. User selects a rule.
3. User changes the rule category.
4. Spendnest shows matching transactions and their current category.
5. User saves the rule or saves and applies it to existing transactions.

## UI

- Add a top-level Rules item between Transactions and Settings.
- Add a Rules page with a compact rules list and a selected-rule detail panel.
- The rules list shows pattern, match type, category, and match count.
- The detail panel shows the editable category selector and preview area.
- The preview table shows date, description, card, amount, current category, and new category.
- Use the existing light workspace, dense tables, white panels, green primary buttons, and amber warning copy for retroactive changes.

## Required Mockup Content

- Show the left sidebar with Rules selected.
- Show a Rules page header with a short subtitle and a small total-rules or matched-transactions summary.
- Show a compact left rules list with realistic examples:
  - DOORDASH, Prefix, Restaurants & Coffee, 38 matches.
  - COSTCO, Prefix, Groceries, 14 matches.
  - PARKING, Contains, Transportation, 9 matches.
  - NETFLIX, Exact, Subscriptions, 6 matches.
- Show the selected rule detail panel for DOORDASH.
- Show non-editable pattern and match type fields, plus an editable category selector.
- Show two actions: Save rule and Save & apply.
- Show an amber confirmation/impact note explaining that Save & apply moves existing matching imported transactions.
- Show a preview table with before/after category movement for several transactions.
- Include zero-match, loading, success, and error state treatments only if they can be shown compactly without cluttering the main mock.

Avoid a marketing page, oversized hero layout, decorative gradients, nested cards, advanced rule criteria, rule deletion, rule ordering, custom categories, or unrelated settings UI.
