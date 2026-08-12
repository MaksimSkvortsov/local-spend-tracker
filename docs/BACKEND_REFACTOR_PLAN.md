# Backend Refactor Plan

Created: 2026-08-11

This is the orchestrator queue for service/infra backend cleanup. Process one class at a time with `$refactor-backend`, update the row, review/fix, run focused tests, stage the completed slice, and stop.

Rows are `Complete` only after that class goes through a `$refactor-backend` slice. Moves, deletions, and supporting architecture changes do not by themselves count as completed refactors.

Cross-cutting note: `FileUploadProgress` reporting is currently spread across `StatementFileImportService`, `TransactionCategorizationService`, and the MAUI import page. Revisit this during the relevant `$refactor-backend` slices to decide whether progress reporting needs a clearer application-level home.

## Console Command Surface

| Class name | Refactoring status | Date | Notes |
| --- | --- | --- | --- |
| SpendnestConsoleApp | Not started | 2026-08-11 | `src/Spendnest.Console/SpendnestConsoleApp.cs` |
| SpendnestCommandDispatcher | Not started | 2026-08-11 | `src/Spendnest.Console/SpendnestCommandDispatcher.cs` |
| CommandLineTokenizer | Not started | 2026-08-11 | `src/Spendnest.Console/CommandLineTokenizer.cs` |
| ConsoleEnvironment | Not started | 2026-08-11 | `src/Spendnest.Console/ConsoleEnvironment.cs` |
| ReportMonth | Not started | 2026-08-11 | `src/Spendnest.Console/ReportMonth.cs` |

## Application Services

| Class name | Refactoring status | Date | Notes |
| --- | --- | --- | --- |
| TransactionCategorizationService | Not started | 2026-08-12 | Moved to `src/Spendnest.Application/Categorization/TransactionCategorizationService.cs`; still needs a `$refactor-backend` slice. |
| TransactionCategorizationApplier | Not started | 2026-08-12 | Moved to `src/Spendnest.Application/Categorization/TransactionCategorizationApplier.cs`; interface moved to Application; still needs a `$refactor-backend` slice. |
| StatementFileImportService | Complete | 2026-08-12 | Refactored `src/Spendnest.Application/Importing/StatementFileImportService.cs`; focused tests passed: `dotnet test src\Spendnest.Application.Tests\Spendnest.Application.Tests.csproj --filter FullyQualifiedName~StatementFileImportServiceTests`; reviewer passed with no code findings. |
| CategorySpendingReportService | Not started | 2026-08-12 | Moved to `src/Spendnest.Application/Reporting/CategorySpendingReportService.cs`; still needs a `$refactor-backend` slice. |
| TransactionReviewService | Not started | 2026-08-12 | Moved to `src/Spendnest.Application/Review/TransactionReviewService.cs`; still needs a `$refactor-backend` slice. |

## Categorization Infrastructure

| Class name | Refactoring status | Date | Notes |
| --- | --- | --- | --- |
| LocalTransactionCategorizer | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Categorization/LocalTransactionCategorizer.cs` |
| OpenAiConnectionTestService | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Categorization/OpenAiConnectionTestService.cs` |
| OpenAiTransactionCategorizer | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Categorization/OpenAiTransactionCategorizer.cs` |
| StoredOpenAiTransactionCategorizer | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Categorization/StoredOpenAiTransactionCategorizer.cs` |
| TransactionMerchantCodeResolver | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Categorization/TransactionMerchantCodeResolver.cs` |

## Repositories

| Class name | Refactoring status | Date | Notes |
| --- | --- | --- | --- |
| SqliteCardAccountRepository | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Accounts/SqliteCardAccountRepository.cs` |
| SqliteCategoryRepository | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Categories/SqliteCategoryRepository.cs` |
| SqliteCategoryRuleRepository | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Categorization/SqliteCategoryRuleRepository.cs` |
| SqliteTransactionCategoryAssignmentRepository | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Categorization/SqliteTransactionCategoryAssignmentRepository.cs` |
| SqliteStatementImportRepository | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Importing/SqliteStatementImportRepository.cs` |
| SqliteTransactionRepository | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Transactions/SqliteTransactionRepository.cs` |

## Importing Infrastructure

| Class name | Refactoring status | Date | Notes |
| --- | --- | --- | --- |
| CsvStatementParser | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Importing/CsvStatementParser.cs` |
| LocalStatementFileReader | Not started | 2026-08-12 | `src/Spendnest.Infrastructure/Importing/LocalStatementFileReader.cs` |

## Persistence, Logging, and Credentials

| Class name | Refactoring status | Date | Notes |
| --- | --- | --- | --- |
| InMemoryCredentialStore | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Credentials/InMemoryCredentialStore.cs` |
| SpendnestFileLogger | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Logging/SpendnestFileLogger.cs` |
| SpendnestFileLoggerProvider | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Logging/SpendnestFileLoggerProvider.cs` |
| SpendnestFileLoggingBuilderExtensions | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Logging/SpendnestFileLoggingBuilderExtensions.cs` |
| ServiceCollectionExtensions | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Persistence/ServiceCollectionExtensions.cs` |
| SpendnestDatabaseInitializer | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Persistence/SpendnestDatabaseInitializer.cs` |
| SpendnestDataPaths | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Persistence/SpendnestDataPaths.cs` |
| SpendnestDbContext | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Persistence/SpendnestDbContext.cs` |
| SpendnestDbContextFactory | Not started | 2026-08-11 | `src/Spendnest.Infrastructure/Persistence/SpendnestDbContextFactory.cs` |

## Core Helpers

| Class name | Refactoring status | Date | Notes |
| --- | --- | --- | --- |
| StatementAmountNormalizer | Not started | 2026-08-11 | `src/Spendnest.Core/Transactions/StatementAmountNormalizer.cs` |
| TransactionFingerprint | Not started | 2026-08-11 | `src/Spendnest.Core/Transactions/TransactionFingerprint.cs` |
| TransactionQuery | Not started | 2026-08-11 | `src/Spendnest.Core/Transactions/TransactionQuery.cs` |
