# Card Management Mockup Notes

Date: 2026-09-13

## Source

- Feature design: `docs/features/card-management/design.md`
- Stitch prompt: `docs/ui-mockups/card-management/stitch-prompt.md`
- Stitch project: PeasantMoney, `14634745227908556953`
- Stitch workflow: generate a new desktop screen using existing project screenshots as visual references.

## Environment

- `STITCH_API_KEY`: present
- `STITCH_PROJECT_ID`: present

## Assumptions

- The user requested a Stitch UI mockup/handoff, not a local HTML artifact.
- The canonical product source is `design.md`; there is no separate `requirements.md` for this feature.
- The generated screen should show the loaded state plus at least one destructive confirmation example.

## Stitch Result

- Initial generated screen: `projects/14634745227908556953/screens/1ec9c8d255904e38a14a89c6137175c0`
- Correction retry used: yes.
- Default loaded-state screen: `projects/14634745227908556953/screens/4303e3b02bdb450dafc4425b5e3745ad`
- Delete confirmation screen: `projects/14634745227908556953/screens/05a74a533c7f4474ba5510e5204c99f0`
- Earlier default screen with redundant visuals: `projects/14634745227908556953/screens/1d21111078504e6784e132e39cd912bd`
- Default screen title: `Cards - PeasantMoney (Default View)`
- Confirmation screen title: `Cards Management - PeasantMoney`
- Project link: `https://stitch.withgoogle.com/projects/14634745227908556953?pli=1`

## Requirements vs Mockup Review

- Missing or unclear after correction: initial corrected result only showed the Cards page with delete confirmation open, so a separate default loaded-state variant was generated.
- Visual issue found after screenshot validation: the first default variant still had redundant duplicate-management visuals, including a top amber duplicate banner, duplicate table badge, combine panel, "Deduplication" chip, and bottom footnote.
- Current coverage: clean default Cards page, card table, combine flow, and delete confirmation state.
- Added or overcomplicated in first generation: header Add/Detect and Export actions, long destructive copy, transaction counts in delete action text, acknowledgement checkbox, and local SQLite/encryption details.
- Correction result: simplified the delete confirmation, removed extra header actions, removed checkbox and technical database copy, then regenerated a clean default no-modal page without the redundant warning banner, deduplication chip, or footer note.

## Validation

- Reviewed Stitch MCP output against `docs/features/card-management/design.md`.
- Downloaded and visually inspected Stitch screenshots locally:
  - `artifacts/stitch-card-management/cards-default-clean-fresh.png`
  - `artifacts/stitch-card-management/cards-confirmation.png`
- Checked the fresh default HTML for redundant strings such as `Deduplication`, `SQLite`, `Export`, and `Add / Detect`; none were found.
- Local browser validation was not run because no local HTML artifact was requested or created.

## Open Questions

- Should the Transactions card filter include a shortcut from the Cards page?
