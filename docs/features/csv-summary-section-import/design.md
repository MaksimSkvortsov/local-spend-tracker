# Feature: Parse CSV Statements With Summary Sections

Date: 2026-10-02
Status: Proposed

## Summary

Some bank CSV files place a summary table before the transaction table. The shared CSV parser should find the transaction header after the summary and parse the transaction rows. Desktop and console imports should benefit from the same parser change.

## UX

- Primary flow: Select or import the CSV through the existing desktop or console flow.
- Placement: Existing Import page and console commands.
- States: Existing preview, import, and warning states.
- Controls: No new controls.

## UI

- Screen changes: None.
- Layout: Unchanged.
- Components: Unchanged; existing row warnings report transactions that cannot be parsed.
- Visual treatment: Unchanged.

## Requirements

- The parser should locate a transaction header containing the required date, description, and amount columns, or date, description, debit, and credit columns, after optional summary rows and blank lines.
- Rows before the transaction header must not be parsed as transactions. The parser should keep original CSV line numbers for parsed rows and warnings.
- A transaction row with a missing amount must be skipped with a row warning. The parser must not infer its amount from a running balance or summary total.
- Files whose transaction header is the first row must continue to parse as before. If no valid transaction header is found, return a clear parse warning instead of guessing.
- The same parser behavior must serve desktop preview/import and console parse/import.

## Acceptance Criteria

- For the motivating `stmt.csv`, the parser ignores the summary section, selects the transaction header on line 7, parses 13 transaction rows, and reports line 8 as skipped because its Amount is blank.
- The parsed transactions retain their source line numbers, descriptions, dates, and normalized amounts; no summary row becomes a transaction.
- Existing single-table CSV fixtures continue to produce the same parsed rows and warnings.
- A CSV without a valid transaction header produces no transactions and a clear missing-header warning.

## Out of Scope

- New import screens, controls, confirmation steps, or history statuses.
- Filling in missing transaction amounts or editing CSV data in the app.
- Changing how an import with zero parsed rows is recorded in Import History; that is a separate issue in the import workflow.

## Open Questions

- None.
