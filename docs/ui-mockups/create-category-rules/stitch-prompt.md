# Stitch Prompt: Create Category Rules

Use project `14634745227908556953` / `https://stitch.withgoogle.com/projects/14634745227908556953?pli=1`.

Use screenshots already present in the Stitch project as visual references for Spendnest/PeasantMoney layout, density, navigation, styling, and component treatment. Use the existing Stitch screen `Rules - PeasantMoney` as the baseline to extend, not replace.

Source design doc: `docs/features/create-category-rules/design.md`.

Generate a desktop-first web app mockup for adding a new category rule from the existing Rules page.

## Feature Summary

The implemented Rules page lets users view, edit, preview, and apply saved category rules, but creating new rules is still out of scope. Add a focused creation flow so users can define a new pattern, match type, and category from the Rules page, preview the historical impact, and save the rule for both existing and future imported transactions.

Rules keep the current model: `Pattern`, `MatchType`, and `CategoryId`.

## UX

- Primary flow: User opens Rules, selects New Rule, enters pattern, match type, and category, reviews impact, confirms when existing transactions will move, then creates and applies the rule.
- Placement: Add New Rule as a primary action in the Rules page header or saved-rules list toolbar.
- States: New draft, validation, preview loading, zero matches, duplicate blocked, broad-impact warning, ready to confirm, success, partial failure, and error.
- Controls: Pattern input, match type selector, category selector, Cancel, and Create & Apply.

## UI

- Screen changes: The Rules page supports a creation state in the existing detail panel.
- Layout: New Rule clears rule selection and shows the same detail and preview layout used for editing, with an unsaved draft label.
- Components: Add a primary New Rule button, field-level validation, impact summary, preview table, amber retroactive-change warning, confirmation copy, and result message.
- Visual treatment: Keep the dense desktop utility style, green primary action, category chips, compact controls, and calm warning copy already used by Rules.

## Required Mockup Content

- Show the left sidebar with Rules selected.
- Show the Rules page header with a green New Rule button.
- Show the existing compact saved-rules list on the left, with New Rule active as an unsaved draft state.
- Show the detail panel in create mode with:
  - title `New category rule`
  - draft/unsaved label
  - Pattern input containing `LYFT`
  - Match type selector set to `Contains`
  - Category selector set to `Transportation`
  - Cancel button
  - green Create & Apply button
- Show validation affordances without making the main state look broken, such as helper copy under Pattern and a compact duplicate-rule warning example in a secondary inline state.
- Show an impact summary above the preview:
  - `12 matched transactions`
  - `10 would change category`
  - `$248.76 affected spend`
  - `2 already Transportation`
- Show an amber confirmation note:
  - `Creating this rule will move 10 existing matching transactions to Transportation. Future imports that match LYFT will use this category automatically.`
- Show a dense preview table with columns:
  - Date
  - Description
  - Card
  - Amount
  - Current category
  - New category
  - Result
- Use realistic preview rows:
  - `2026-08-29`, `LYFT *RIDE 08-29`, `Chase Freedom`, `$18.42`, `Uncategorized`, `Transportation`, `Will change`
  - `2026-08-18`, `LYFT TRIP HELP.LYFT.COM`, `Amex Blue`, `$31.80`, `Travel`, `Transportation`, `Will change`
  - `2026-08-03`, `LYFT BIKE SHARE`, `Chase Freedom`, `$9.99`, `Transportation`, `Transportation`, `Already set`
  - `2026-07-21`, `LYFT *RIDE 07-21`, `Capital One`, `$24.60`, `Personal`, `Transportation`, `Will change`
- Include a compact broad-impact warning state, such as a small right-side warning or preview banner for `146 would change`.
- Include a zero-match treatment if it fits without clutter:
  - `No existing transactions match. Create the rule to categorize future imports automatically.`
- Include a success toast or inline status:
  - `Rule created. 10 transactions updated.`
- Include a partial-failure status treatment compactly:
  - `Rule created, but transaction updates did not finish. Retry apply.`

## Design Constraints

- Match Spendnest's existing light workspace, fixed left sidebar, white bordered panels, compact controls, dense table rows, green primary action, amber warning copy, and category chips.
- Keep the screen table-first and efficient. Do not use a marketing hero, decorative gradients, oversized typography, nested cards, custom category creation, rule deletion, rule ordering, enable/disable controls, or advanced rule criteria.
- Ensure the mockup makes clear that matched transactions and transactions that would actually change are different concepts.
- Make the primary next step clear without overwhelming the user with modal-heavy UI.
- Use desktop-first proportions, but keep a plausible narrow viewport adaptation in mind.
