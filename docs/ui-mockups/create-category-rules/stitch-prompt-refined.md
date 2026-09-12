# Stitch Prompt: Create Category Rules Refined Default Flow

Use project `14634745227908556953` / `https://stitch.withgoogle.com/projects/14634745227908556953?pli=1`.

Refine the existing `New Category Rule - PeasantMoney` mockup to reduce complexity while preserving the create-rule requirements.

## Goal

Make the default create-rule state feel simple and implementable. Show only the normal create flow on the main screen. Do not display success, zero-match, broad-impact, duplicate warning, and partial-failure states all at the same time.

## Keep

- Left sidebar with Rules selected.
- Rules page header with a green `New Rule` button.
- Saved Rules list with an active draft item for `LYFT`.
- Detail panel title `New category rule` with `Unsaved Draft` badge.
- Required fields: Pattern = `LYFT`, Match Type = `Contains`, Category = `Transportation`.
- Helper copy under Pattern about matching statement merchant text case-insensitively.
- Compact impact summary: `12 matched`, `10 will change`, `$248.76 affected spend`.
- Small muted note for `2 already Transportation`.
- Amber confirmation note: `Creating this rule will move 10 existing matching transactions to Transportation. Future imports that match LYFT will use this category automatically.`
- Action row with only `Cancel` and green primary `Create & Apply to 10 Transactions`.
- Dense preview table with Date, Description, Card, Amount, Current Category, New Category, Result, using realistic LYFT rows.

## Drop From Default Main Screen

- Success toast/banner.
- Positive duplicate-check success callout.
- Broad-impact warning pill/callout.
- Zero-match note.
- Partial-failure recovery badge.
- Visible state switcher or recovery notes section.
- `Save Rule Only` action.

## Design Constraints

Stay with Quiet Finance style, white bordered panels, compact controls, dense table rows, green primary action, amber warning copy, no nested cards, no decorative visuals, no oversized typography.
