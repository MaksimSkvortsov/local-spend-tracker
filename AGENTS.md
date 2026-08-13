# Spendnest Codex Guidance

Backend cleanup is incremental. When asked to refactor backend code, handle exactly one primary production class per invocation and stop when that class is complete.

Use the `refactor-backend` skill for backend refactoring work. Before changing code, read the target class, its direct collaborators, and its existing tests to understand current behavior and boundaries.

Preserve observable behavior unless the user explicitly requests a behavior change. Keep edits scoped to the target class; make small supporting changes to direct collaborators only when required for the refactor.

Clean architecture placement matters. Application/use-case orchestration belongs in `src/Spendnest.Application`, including services that coordinate Core interfaces or map use-case results into repository operations. Infrastructure should contain concrete external details such as SQLite repositories, file-system readers, OpenAI/HTTP clients, credential storage, logging, and persistence wiring. Test-only fakes belong under test projects, not production Infrastructure.

Keep workflow application services focused on orchestration. When complicated pure logic appears inside a workflow service, extract it into a concrete application/domain collaborator that can be tested directly without repository fakes, like `CategorySpendingReportBuilder`. Do not add an interface unless there is a real boundary or runtime variation.

For multi-class backend cleanup, create and maintain `docs/BACKEND_REFACTOR_PLAN.md` as the orchestrator queue. Each row tracks class name, refactoring status, date, and notes. Process one row at a time.

After implementation, run the narrowest relevant `dotnet test` command, spawn the reviewer subagent for an independent diff review, fix meaningful findings, update the plan row, re-run relevant tests when production code changes, and stage only the completed slice.

Do not automatically continue to another class.
