# Create Category Rules Notes

Date: 2026-09-11

## Source Context

- Existing design: `docs/features/category-rules-management/design.md`
- Existing status: Category Rules Management is implemented for viewing, editing, previewing, and applying saved rules.
- Existing out-of-scope item: Creating new rules from the Rules page.

## Persona Feedback Applied

- Added exact duplicate blocking because both reviewers flagged duplicate pattern and match type combinations as a trust risk.
- Added preview separation between matched transactions and transactions that would actually change.
- Added impact summary with counts and total amount grouped by category movement.
- Added explicit confirmation when existing transactions will move.
- Added zero-match copy that explains future-import behavior.
- Added result messaging that distinguishes rule creation from transaction updates.
- Added partial-failure acceptance criteria for the case where the rule is saved but transaction updates fail.
- Added broad-impact warning requirement, using 100 changed transactions as the first implementation threshold.

## Feedback Not Added

- Showing a new rule's effective priority position was not added as a separate requirement. The current design keeps conflict resolution out of scope, so this proposal keeps priority visible through preview results rather than a dedicated priority model.
- Category group or parent labels were not added because the current category model in the referenced design only requires category chips/selectors, and no grouped category taxonomy is part of this feature.
