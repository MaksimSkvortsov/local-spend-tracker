# Implementation Plan: Category Rules Management

## Scope

- Add the minimal Rules page for viewing rules, filtering the rule list, editing pattern/match type/category, previewing matching transactions, and always applying the saved rule to existing matching transactions.

## External Links

- Requirements: requirements.md
- Design: design.md
- Stitch mockup: `projects/14634745227908556953/screens/cb050bb964ce4269b52c98576e17fa0c`
- Downloaded Stitch reference: `docs/ui-mockups/category-rules-management/stitch-screen.png`

## Backend Plan

- Use existing rule matching behavior for preview and apply: Exact, Prefix, Contains, longer pattern, then earlier-created rule.
- Add an application service for loading rules, building transaction previews, saving rule edits, and applying the saved rule.
- Add an atomic persistence operation that updates the rule and matching transaction assignments together.
- Save assignments for all transactions where the selected rule is the current winning rule.

## Frontend Plan

- Add Rules as a top-level navigation item.
- Add a desktop-first Rules page with a filtered, scrollable saved-rules list.
- Show each rule with pattern, match type, category chip, and match count.
- Add selected rule details with editable pattern input, match type selector, and category selector.
- Keep the Pattern input visually aligned with adjacent white form controls.
- Use one action: Save & Apply to All.
- Show status messages, simple loading indicators, and a preview table with current matching transactions.

## Validation

- Run focused application/infrastructure tests affected by repository and categorization contracts.
- Run the UI harness build.
- Verify the Saved Rules list and category chip layout with Playwright.
- Run independent diff review.

## Risks

- The UI does not warn about overlapping rules. Existing rule priority decides which rule wins.
- The Rules page does not support creating, deleting, reordering, enabling, or disabling rules.
