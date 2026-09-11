# Design: Category Rules Management

Date: 2026-09-08
Status: Implemented

## Summary

Spendnest already creates category rules, but users cannot manage them. Add Rules as a top-level page where users can view saved rules, filter them, edit a selected rule, preview matching transactions, and apply the saved rule to those transactions.

This is the minimal version. Rules keep the current model: `Pattern`, `MatchType`, and `CategoryId`.

## User Flow

1. User opens Rules from the sidebar.
2. User selects a rule.
3. User changes the rule pattern, match type, or category.
4. Spendnest shows matching transactions and their current category.
5. User saves the rule, and Spendnest applies it to existing matching transactions.

## UI

- Add a top-level Rules item between Transactions and Settings.
- Add a Rules page with a compact rules list and a selected-rule detail panel.
- The rules list shows pattern, match type, category, and match count.
- The rules list includes a filter and scrolls when there are many rules.
- The detail panel shows editable pattern, match type, and category controls.
- The preview table shows date, description, card, amount, and current category.
- Use the existing light workspace, dense tables, white panels, green primary buttons, and amber warning copy for retroactive changes.
- Show simple loading indicators when rules, rule details, or the preview are loading.

## Behavior

### View Rules

Users can see all saved rules and quickly understand what each rule matches and which category it assigns. Users can filter by pattern, match type, or category.

### Edit Rule

Users can edit the selected rule pattern, match type, and assigned category. Rule order and rule deletion are later features.

### Save Rule

Saving updates the rule and moves all currently matching imported transactions to the rule category.

Before applying, Spendnest shows a preview and clear confirmation text:

```text
Saving will move 38 existing matching transactions to Restaurants & Coffee. If there are no matches, only the rule changes.
```

If there are no matches, users can still save the rule, but no existing transactions move.

## Rule Matching

Use the existing rule matching behavior:

- `Exact` matches the resolved merchant code exactly.
- `Prefix` matches the start of the resolved merchant code.
- `Contains` matches the original transaction description.
- Matching is normalized with trim and uppercase.
- When multiple rules match, current priority wins: Exact, then Prefix, then Contains, then longer pattern.
- If rules still tie, the earlier-created rule wins.

The preview and apply operation must use the same matching logic.

The UI does not show conflict warnings in this version. If an edited rule overlaps another rule, only the transactions where the edited rule is the current winning rule appear in preview and get updated.

## Requirements

### Must Have

- Top-level Rules page.
- Rule list with filter, category chip, and match count.
- Scrollable saved-rules section.
- Selected-rule detail panel.
- Pattern, match type, and category edit controls.
- Matching transaction preview.
- Save and apply to existing transactions.
- Loading, empty, success, and error states.

### Must Be Clear

- Applying to existing transactions changes historical category totals.
- The preview and apply operation must use the same rule matching priority.
- Errors must say whether anything changed.

## Acceptance

The feature is ready when a user can open Rules, pick a rule, change it, preview matching transactions, and save it so matching imported transactions are updated.

It must also handle empty rules, zero matching transactions, preview refresh, success, and failure states without leaving the user unsure about what changed.

## Out Of Scope

- Creating new rules from this page.
- Deleting rules.
- Reordering rules.
- Enable/disable rules.
- Conflict warnings or conflict resolution UI.
- Undo history.
- Custom categories.

## Open Questions

- Should the success message link to the affected Transactions filter?
