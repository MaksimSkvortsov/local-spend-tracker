# Implementation Plan: Create Category Rules

## Scope

- Add the create-rule slice to the existing Rules page: draft a rule, preview matching transactions, page through preview rows, create it, and apply it to existing winning matches.

## External Links

- Design: design.md
- Stitch mockup: `projects/14634745227908556953/screens/2f940655b35b45dcb29e852951b93af7`
- Downloaded Stitch reference: `docs/ui-mockups/create-category-rules/stitch-screen.png`

## Backend Plan

- Add a create-rule command for pattern, match type, and category.
- Reuse existing matching logic for create preview and create/apply.
- Block exact duplicate normalized pattern plus match type before saving.
- Add an atomic store operation that inserts the rule and matching transaction assignments together.

## Frontend Plan

- Add a New Rule action and draft state to the existing Rules page.
- Reuse the existing detail form for pattern, match type, and category.
- Show compact impact metrics and a paginated preview table with current category, new category, and result.
- Keep zero-match, duplicate, success, and failure messaging triggered by state rather than always visible.
- Keep create-mode cancellation in the footer action area only.
- Align same-row rule form controls by top position and height.

## Validation

- Run focused application tests for rule management.
- Run focused infrastructure persistence tests.
- Build the desktop project.
- Run Playwright/browser verification for create/apply behavior, visual alignment, preview pagination, and create-mode actions.
- Run independent diff review.

## Risks

- Existing tie behavior still depends on rule list order after match type and pattern length priority.
- Broad-impact warning uses 100 changed transactions as the first implementation threshold.
