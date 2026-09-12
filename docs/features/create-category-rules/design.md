# Feature: Create Category Rules

Date: 2026-09-11
Status: Implemented

## Summary

The existing Rules page lets users view, edit, preview, and apply saved category rules, but creating new rules is still out of scope. Add a focused creation flow so users can define a new pattern, match type, and category from the Rules page, preview the historical impact, and save the rule for both existing winning matches and future imported transactions.

## UX

- Primary flow: User opens Rules, selects New Rule, enters pattern, match type, and category, reviews impact, then creates and applies the rule.
- Placement: Add New Rule as a primary action in the Rules page header; show the draft at the top of the saved-rules list while create mode is active.
- States: New draft, validation, preview loading, zero matches, duplicate blocked, broad-impact warning, ready to create, success, and error.
- Controls: Pattern input, match type selector, category selector, Cancel, and Create & Apply.

## UI

- Screen changes: The Rules page should support a creation state in the existing detail panel.
- Layout: New Rule should clear rule selection and show the same detail and preview layout used for editing, with an unsaved draft label and one footer Cancel action.
- Components: Add a primary New Rule button, draft list item, field-level validation, impact summary, paginated preview table, amber retroactive-change warning, action copy, and result message.
- Visual treatment: Keep the dense desktop utility style, green primary action, category chips, compact controls, and calm warning copy already used by Rules.

## Requirements

- Users can create a rule with Pattern, MatchType, and CategoryId from the Rules page.
- Pattern cannot be blank after trimming, category must be selected, and an identical normalized pattern plus match type must be blocked before save.
- Preview must use the same normalization and priority behavior as saved rule editing and application.
- Preview must separate transactions the new rule matches from transactions whose category would actually change because the new rule wins.
- Preview must show the count and total amount that would change, grouped by current category and new category.
- Preview must show how many winning matches are already in the selected category so match count and change count are easy to reconcile.
- Preview must paginate matching transactions in pages of 10 when more than 10 rows match.
- If one or more existing transactions will change, the impact note and primary action must state how many transactions will move to the selected category.
- If a draft rule would change 100 or more transactions, the UI must show a stronger broad-impact warning before confirmation.
- If there are no matching transactions, users can still create the rule and the UI must explain that it will apply to future imports only.
- Create & Apply saves the new rule and updates imported transactions where the new rule is the current winning rule.
- Create & Apply must be atomic: if the save/apply operation fails, the UI reports that nothing changed.
- Result messages must separately state whether the rule was created and how many transactions were updated.

## Acceptance Criteria

- A user can click New Rule, fill pattern, match type, and category, preview impact, confirm when needed, and create the rule.
- Validation prevents saving an incomplete, blank, or exact duplicate rule without losing draft input.
- For the same draft rule, the previewed would-change transaction set matches the transaction set updated after Create & Apply.
- Zero-match rules can be saved with clear copy that no existing transactions changed and future imports can match.
- After successful creation, the new rule is selected, the rules list is refreshed, and the preview reflects post-apply categories.
- When the preview has more than 10 matching transactions, users can move between preview pages and the summary shows the visible count, total count, and current page.
- If rule creation fails, the UI states that nothing changed and keeps the draft available for correction or retry.

## Out of Scope

- Creating rules directly from transaction rows.
- Deleting rules.
- Reordering rules.
- Enable/disable rules.
- Conflict warnings beyond exact duplicate blocking and preview impact clarity.
- Custom categories.

## Open Questions

- Should manually assigned categories be eligible for Create & Apply updates, or only rule-assigned/imported categories?
- Should the "already in selected category" summary be hidden for unchanged saved-rule views and shown only for create/edit impact review?
