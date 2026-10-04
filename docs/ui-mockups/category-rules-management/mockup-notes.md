# Mockup Notes: Category Rules Management

Source design doc: `docs/features/category-rules-management/design.md`

Prompt sent to Stitch: `docs/ui-mockups/category-rules-management/stitch-prompt.md`

Stitch project: `14634745227908556953`

Stitch project URL: `https://stitch.withgoogle.com/projects/14634745227908556953?pli=1`

Generated screen: `projects/14634745227908556953/screens/cb050bb964ce4269b52c98576e17fa0c`

Generated screen title: `Rules - PeasantMoney`

Design system used: `assets/f509c81516264d138439a9c7b26b94d5` (`Quiet Finance`)

## What Was Generated

Stitch generated a desktop Rules page mockup with:

- Rules as a top-level sidebar item between Transactions and Settings.
- A compact page header with rule and transaction summary.
- A two-column master-detail layout.
- A left rules list showing pattern, match type, category, and match count.
- A selected DOORDASH rule detail panel with read-only pattern and match type fields.
- An editable category selector.
- Separate `Save Rule` and green `Save & Apply to 38 Transactions` actions.
- An amber impact note for applying category changes to existing transactions.
- A dense matching transactions preview table with current category and new category.

## Assumptions

- The first version only edits a rule's assigned category.
- Pattern and match type remain read-only.
- Applying a rule updates all currently matching imported transactions.
- Creating categories, deleting rules, editing rule patterns, and confirmation modals are outside the minimal version.

## Stitch Suggestions

Stitch suggested these optional follow-ups:

- Add inline new-category creation from the dropdown.
- Explore the zero matching transactions empty preview state.
- Add a bulk reassignment confirmation modal before applying.

These are not included in the minimal mock unless requested later.
