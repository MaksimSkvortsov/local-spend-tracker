# Mockup Notes: Create Category Rules

Source design doc: `docs/features/create-category-rules/design.md`

Prompt prepared for Stitch: `docs/ui-mockups/create-category-rules/stitch-prompt.md`

Refinement prompt: `docs/ui-mockups/create-category-rules/stitch-prompt-refined.md`

Stitch project: `14634745227908556953`

Stitch project URL: `https://stitch.withgoogle.com/projects/14634745227908556953?pli=1`

Baseline Stitch screen available in project: `Rules - PeasantMoney` (`projects/14634745227908556953/screens/cb050bb964ce4269b52c98576e17fa0c`)

## Status

Stitch generated an initial desktop variant from the existing `Rules - PeasantMoney` screen, then a refined default-flow variant to reduce complexity.

- Initial generated screen: `projects/14634745227908556953/screens/3d359b513e074bc4ab7b2c1cdaee239f`
- Refined generated screen: `projects/14634745227908556953/screens/2f940655b35b45dcb29e852951b93af7`
- Refined screen title: `New Category Rule - Default Flow`
- Design system used: `assets/f509c81516264d138439a9c7b26b94d5` (`Quiet Finance`)
- Downloaded screenshot: `docs/ui-mockups/create-category-rules/stitch-screen.png`
- Downloaded HTML export: `docs/ui-mockups/create-category-rules/stitch-screen.html`

## What Was Generated

- Rules remains selected in the left sidebar.
- The Rules page header includes a green New Rule action and compact rule/match summary.
- The saved-rules list shows an active `[DRAFT] LYFT` item.
- The detail panel shows `New category rule` with an Unsaved Draft badge.
- The create form includes Pattern, Match Type, and Assign Category controls.
- The impact summary distinguishes matched transactions from transactions that would change.
- Amber confirmation copy explains the retroactive move and future-import behavior.
- The action row includes Cancel and Create & Apply to 10 Transactions.
- The matching transactions preview table shows current category, new category, and result.
- Conditional states such as zero matches, broad impact, duplicate blocking, success, and partial failure are not shown in the default flow.

## Review Outcome

- Valuable: New Rule entry point, active draft row, required fields, compact impact summary, amber confirmation, and matching transaction preview.
- Dropped from default view: Success toast, positive duplicate-check message, broad-impact warning, zero-match note, partial-failure badge, state-notes section, and Save Rule Only button.
- Keep for implementation states: duplicate blocking, zero-match copy, broad-impact warning, success message, and partial-failure retry, but show them only when triggered.

## Assumptions

- The mockup extends the existing Rules page rather than creating a separate page.
- Exact duplicate pattern plus match type is blocked in the creation flow.
- Overlap/conflict resolution remains out of scope beyond preview impact clarity.
- The broad-impact threshold remains open and should be represented visually without hardcoding a final number.

## Validation

- Verified the source design exists.
- Verified Stitch project context exists in `docs/STITCH.md`.
- Verified the PeasantMoney Stitch project contains the existing Rules baseline screen.
- Verified the mockup handoff uses the feature `design.md` artifact.
- Downloaded and visually inspected the generated screenshot.
