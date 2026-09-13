# Frontend Page Structure Guidelines

Razor pages in Spendnest should be thin composition surfaces. A page should make the user workflow visible, coordinate page-level state, and delegate repeated or meaningful UI concepts to feature components and shared primitives.

Use `Rules.razor` as a cautionary example: it became much larger than peer pages because the page carried saved-list markup, edit-form markup, impact summaries, preview state, validation labels, and workflow helpers inline. The healthier pattern is visible in `Transactions.razor`, `Import.razor`, `Settings.razor`, and `Dashboard.razor`, where pages mostly compose feature components.

## Preferred Shape

Prefer this structure:

```text
Page
  -> feature composition
  -> feature components
  -> shared UI primitives
```

Pages may own:

- route declarations, injected page services, and page title.
- top-level layout decisions and page-level cards or sections.
- loading, empty, error, and selected-item state that spans multiple child components.
- calls to page/application services and cross-page notifications.
- orchestration for save, create, delete, filter, and pagination actions when those actions affect more than one child component.

Pages should avoid owning:

- full table markup when a shared or feature table component fits.
- long repeated list/item markup.
- detailed form field layouts that represent a reusable feature concept.
- presentation-heavy summaries, badges, chips, warnings, or result labels.
- complex derived display values that can be modeled as presentation state or owned by a feature component.
- many mutable draft fields and related `CanSave`, `CanSubmit`, validation, dirty-state, and request-building helpers inline in `@code`.

## Component Extraction Triggers

Extract a feature component when a section has its own concept name in the product language, such as a transaction table, rules list, rule detail form, impact summary, import history, or settings panel.

Extraction is usually warranted when any of these are true:

- The page contains a full table, toolbar, form, card body, summary panel, or repeated list item.
- The markup has its own loading, empty, disabled, selected, or validation states.
- The section needs helper methods only to render labels, CSS classes, summaries, or row-level values.
- A page-level `@code` block grows because it is serving a specific UI section rather than the whole page workflow.
- A similar shared primitive already exists, such as `Card`, `DataTable`, `PaginationBar`, toolbar components, settings components, or dashboard widgets.

Do not extract components just to reduce line count. Extract when the new component has a clear responsibility and a stable parameter boundary.

## Page Code-Behind State

Razor page `@code` should coordinate workflow. It should not become the long-term home for every field and derived value needed by a form or editing surface.

When a page has a feature draft with several fields plus validation, dirty-state, enabled-state, or request-building logic, prefer a presentation model near the feature, such as:

- `ImportSelectionDraft`
- `SettingsAiConfigurationDraft`
- a feature-specific draft/state object such as `RuleEditorDraft`

Good page code keeps the application workflow visible:

```text
load data
set selected item
ask draft/state object whether submit is allowed
call page service with draft/state request
refresh data and notify other pages
```

Move logic out of the page when it is mostly about:

- parsing selected IDs from UI strings.
- comparing draft values to a selected baseline.
- computing dirty state, validation visibility, and duplicate checks.
- producing create/update request objects from draft fields.
- exposing display labels that depend only on draft mode and simple counts.

Keep logic in the page when it coordinates services, cross-page notifications, navigation, or state shared by multiple child components.

## Reuse Expectations

Before adding markup for common UI, inspect existing components and shared primitives. Reuse or extend the existing pattern unless there is a concrete reason not to.

Common reuse points include:

- `Card` for framed content.
- `DataTable` and `PaginationBar` for tabular data and paging.
- Existing feature tables such as transaction, import, or rule preview tables.
- Existing settings, dashboard, importing, transaction, and layout components.
- Shared CSS tokens and existing utility classes before introducing one-off styles.

If a new component is needed, place it near its feature area and keep the page API explicit with parameters and callbacks.

## Page Review Checklist

When editing or creating a Razor page, check:

- Does the page mostly compose feature components rather than rendering every section inline?
- Did I inspect existing components before adding table, pagination, card, button, badge, form, toolbar, or list markup?
- Are derived presentation values close to the component that renders them?
- Are form/draft fields and their validation/request-building helpers grouped into a presentation model instead of scattered across the page?
- Is the page coordinating workflow, or is it also doing section-level rendering work that belongs in a feature component?
- Would another page in the app solve this with a child component?

If the answer suggests the page is carrying feature UI inline, extract the smallest coherent component before finishing.
