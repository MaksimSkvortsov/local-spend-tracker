# Implementation Plan: Low-Confidence Transaction Review Flow

## Scope

- Add a minimal Confirm button next to the existing category selector for transactions that need review, so users can accept the preselected category without changing the dropdown.

## External Links

- Requirements: requirements.md
- Stitch mockup: https://stitch.withgoogle.com/projects/14634745227908556953?pli=1

## Backend Plan

- Already stable: `ITransactionReviewService.ConfirmAsync` supports confirming the current category.

## Frontend Plan

- Expose a confirm method from `TransactionsPageService`.
- Add a row-level confirm callback through `Transactions.razor` and `TransactionsTable.razor`.
- Render a compact Confirm button next to the category selector only for `NeedsReview` rows.
- Reuse existing per-row saving and error handling.

## Validation

- Run the narrowest relevant build or tests after the frontend change.

## Risks

- The current saving state is keyed only by transaction ID, so the selector and Confirm button should both disable during either save path.
