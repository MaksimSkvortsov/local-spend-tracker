---
name: refactor-backend
description: Incrementally refactor Spendnest backend C# service/infra classes while preserving behavior. Use when Codex is asked to refactor one backend class, or to run a multi-class backend cleanup campaign across service/infra files using a plan file, with prompts like "Use $refactor-backend on TransactionCategorizer", "Refactor TransactionCategorizer and its tests using $refactor-backend", or "Use $refactor-backend across all service/infra files."
---

# Refactor Backend

Use this skill to clean up backend production classes in Spendnest safely. Even in a multi-class campaign, refactor exactly one primary production class per implementation slice.

## Scope

- Treat the named production class as the primary refactoring unit.
- Work on exactly one primary production class per invocation.
- Include existing tests for that class as part of the unit.
- Read direct collaborators before editing. Direct collaborators include constructor dependencies, implemented interfaces, domain models used in core behavior, repository/service interfaces, and helper classes called by the target class.
- Make small supporting changes to direct collaborators only when required to complete the target refactor correctly.
- Avoid unrelated cleanup, broad architecture redesign, speculative abstractions, unnecessary interfaces, and new dependencies.
- Preserve public and observable behavior unless the user explicitly requests a behavior change.
- In campaign mode, use `docs/BACKEND_REFACTOR_PLAN.md` as the queue and update only the row for the current class during that slice.

## Repository Context

Spendnest is a .NET 10 C# solution. Backend production code is primarily under:

- `src/Spendnest.Application`
- `src/Spendnest.Core`
- `src/Spendnest.Infrastructure`
- `src/Spendnest.Console` when the requested class is part of the backend command surface

Tests use xUnit and FluentAssertions under:

- `src/Spendnest.Application.Tests`
- `src/Spendnest.Core.Tests`
- `src/Spendnest.Infrastructure.Tests`

Common verification commands:

```powershell
dotnet test src/Spendnest.Application.Tests/Spendnest.Application.Tests.csproj
dotnet test src/Spendnest.Core.Tests/Spendnest.Core.Tests.csproj
dotnet test src/Spendnest.Infrastructure.Tests/Spendnest.Infrastructure.Tests.csproj
dotnet test Spendnest.slnx
```

Prefer focused tests first. Use `--filter` for a specific test class or test name when practical, for example:

```powershell
dotnet test src/Spendnest.Infrastructure.Tests/Spendnest.Infrastructure.Tests.csproj --filter FullyQualifiedName~TransactionCategorizationServiceTests
```

Run the whole solution only when the change touches shared contracts, project wiring, persistence behavior with broad blast radius, or focused tests cannot provide adequate confidence.

## Campaign Plan File

When the user asks to work across all service/infra files, create or update `docs/BACKEND_REFACTOR_PLAN.md` before refactoring application code. Use a Markdown table with exactly these columns:

```markdown
| Class name | Refactoring status | Date | Notes |
| --- | --- | --- | --- |
```

Use statuses consistently:

- `Not started`
- `In progress`
- `Reviewing`
- `Fixing review findings`
- `Complete`
- `Skipped`

Populate the initial plan with service/infra candidates from `src/Spendnest.Infrastructure`, backend command-surface classes from `src/Spendnest.Console`, and core backend helper classes from `src/Spendnest.Core` when they support service/infra behavior. Exclude tests, migrations, assembly markers, UI code, generated files, and plain domain/data records unless the user explicitly includes them.

For each class slice:

1. Mark the selected row `In progress` with today's date and a short note.
2. Refactor only that class and its required tests/direct collaborators.
3. Run focused tests.
4. Mark the row `Reviewing` before spawning the reviewer.
5. Spawn the `reviewer` subagent to inspect the diff and plan row.
6. Require the reviewer to check each changed production class individually for pragmatic SOLID compliance, then provide combined prioritized feedback to the developer agent.
7. If meaningful findings exist, mark the row `Fixing review findings`, fix them, and re-run relevant tests after production changes.
8. Mark the row `Complete` with the test command and reviewer outcome, or `Skipped` with the reason.
9. Stage only the completed slice: the target class, related tests, required direct-collaborator changes, and `docs/BACKEND_REFACTOR_PLAN.md`. Do not stage unrelated dirty files.
10. Stop. Do not continue to the next row unless the user explicitly asks for another slice.

## Procedure

1. Identify the requested primary production class. In campaign mode, select exactly one `Not started` plan row for the current slice.
2. Inspect the target class, its direct collaborators, and existing tests before making changes.
3. Summarize concrete maintainability problems before editing. Prefer specific issues such as duplicated branch logic, unclear names, oversized methods, mixed responsibilities, avoidable state, or dead code.
4. Refactor in small steps while preserving observable behavior.
5. Keep public API changes out of scope unless necessary and behavior-preserving for current callers.
6. Update tests only when needed:
   - Preserve existing behavioral coverage.
   - Update implementation-independent expectations when the refactor reveals clearer behavior.
   - Add focused tests for meaningful uncovered behavior discovered during the refactor.
   - Do not rewrite tests merely to match a new internal implementation.
   - Prefer behavior-focused assertions over implementation-detail assertions.
7. Run the narrowest relevant `dotnet test` command.
8. Update `docs/BACKEND_REFACTOR_PLAN.md` when running from a plan.
9. Spawn the `reviewer` subagent to independently inspect the full diff.
10. Fix meaningful reviewer findings.
11. Re-run relevant tests if production code changed after review.
12. Update the plan row when running from a plan.
13. Stage only files in the completed slice, including the plan file when used.
14. Stop after the requested class is complete. Do not continue to another class.

## Refactoring Preferences

## Layering Guidance

Classify production code by responsibility before moving or refactoring it:

- `src/Spendnest.Application`: use-case/application services, orchestration over Core interfaces, application-level interfaces, and mapping use-case results into repository operations.
- `src/Spendnest.Core`: domain models, value objects, domain rules, and stable domain contracts.
- `src/Spendnest.Infrastructure`: concrete external implementations such as SQLite repositories, file-system readers, OpenAI/HTTP clients, credential stores, logging, and persistence wiring.
- Test-only fakes belong in test projects, not production Infrastructure.

If a class depends only on Core abstractions and coordinates application behavior, prefer Application over Infrastructure. If it performs concrete IO or talks to an external system, keep that concrete implementation in Infrastructure behind an Application/Core abstraction.

Keep workflow application services focused on orchestration. If a service mixes repository/workflow coordination with complicated pure business, report-building, matching, mapping, or calculation rules, extract that pure logic into a concrete application/domain collaborator that can be tested directly without repository fakes. Do not introduce an interface for that collaborator unless there is a real boundary, external dependency, or runtime variation.

Favor:

- simpler control flow
- smaller and clearer methods
- clearer naming
- object-oriented modeling that reflects real domain concepts
- removal of duplication
- removal of dead or unnecessary code
- reduced complexity
- clearer responsibilities
- improved testability
- existing repository patterns

Avoid:

- broad architecture redesign
- speculative abstractions
- unnecessary interfaces
- adding interfaces for concrete supporting logic just to satisfy SOLID
- unnecessary dependencies
- unrelated cleanup
- changing neighboring classes just because they could also be improved
- changing public or observable behavior unless explicitly requested
- moving complexity into a helper without actually making responsibility clearer

## SOLID Guidance

Use SOLID as a readability and modeling lens, not an interface-generation checklist. Prefer OOP that gives behavior a clear home in concrete classes, value objects, and well-named methods that reflect the domain.

Application services may have interfaces where they form a boundary for callers, dependency injection, or external integration. The logic an application service relies on usually should not get a new interface. Introduce a new interface only when there is a real boundary, existing repository pattern, external dependency seam, or meaningful runtime variation.

## Reviewer Prompt

After implementation and focused tests, spawn the configured `reviewer` subagent. Ask it to inspect the full diff and answer:

- Was observable behavior preserved?
- Is the resulting code actually simpler and easier to maintain?
- Did the refactor introduce unnecessary abstractions?
- Is responsibility clearer?
- Does each changed production class comply with SOLID in a pragmatic, repository-appropriate way?
- Does the design use OOP to model the domain clearly instead of scattering procedural logic or inventing interfaces?
- Are any SOLID tradeoffs intentional and acceptable for this codebase?
- Are there regressions or edge cases?
- Are tests still meaningful?
- Is important behavior missing test coverage?
- Did the change unnecessarily expand beyond the target class?
- Was complexity merely moved somewhere else?
- Is the `docs/BACKEND_REFACTOR_PLAN.md` row accurate when the plan file is used?

Require the reviewer to provide one combined response to the developer agent, grouped or labeled as behavior preservation, maintainability, SOLID, tests, and plan/status when useful. Require findings to include severity, file/location, problem, why it matters, and suggested fix. If there are no meaningful findings, the reviewer must say the review passed.

## Completion Criteria

- Exactly one primary production class was refactored.
- Existing behavior is preserved.
- Tests for the refactoring unit remain meaningful.
- Focused tests pass, or any inability to run them is reported clearly.
- Reviewer findings are addressed or explicitly dismissed with a reason.
- `docs/BACKEND_REFACTOR_PLAN.md` is updated when running from a plan.
- Only the completed slice is staged when staging is requested by the workflow.
- Work stops after the requested class.
