# Spendnest

Spendnest is a local-first expense tracking app for credit-card spending.

The main app is a Windows MAUI Blazor desktop host branded as PeasantMoney. It imports credit-card CSV statements, stores data in local SQLite, categorizes transactions with saved rules and OpenAI when configured, supports review workflows, and shows spending summaries by category.

The console app remains a permanent command surface for setup, diagnostics, imports, reports, review, and power-user workflows. Both hosts use the same Core and Infrastructure services.

## Requirements

- .NET SDK 10
- MAUI workload for building/running the Windows desktop app

## Projects

```text
src/
  Spendnest.Core
  Spendnest.Infrastructure
  Spendnest.Console
  Spendnest.App
  Spendnest.Core.Tests
  Spendnest.Infrastructure.Tests
```

## Configuration

Spendnest reads configuration from `appsettings.json`, `appsettings.Development.json`, environment variables with the `SPENDNEST_` prefix, and `.env.local`.

In the desktop app, configure the OpenAI API key from `Settings -> AI Configuration`. The key is stored in platform secure storage on the local device.

For the console host, OpenAI categorization can be configured with either:

```powershell
SPENDNEST_OpenAI__ApiKey=<api-key>
```

or:

```powershell
OPENAI_API_KEY=<api-key>
```

The model defaults to `gpt-5.6-luna` and can be overridden with:

```powershell
SPENDNEST_OpenAI__Model=<model>
```

The default SQLite database is stored at:

```text
%LOCALAPPDATA%\Spendnest\spendnest.db
```

Logs are written under:

```text
%LOCALAPPDATA%\Spendnest\logs
```

## Verification

Restore dependencies:

```powershell
dotnet restore Spendnest.slnx
```

Build:

```powershell
dotnet build Spendnest.slnx
```

Run tests:

```powershell
dotnet test Spendnest.slnx
```

Run the Windows app:

```powershell
dotnet run --project src/Spendnest.App
```

Run the console:

```powershell
dotnet run --project src/Spendnest.Console -- help
```

Start an interactive console session:

```powershell
dotnet run --project src/Spendnest.Console -- run
```

Useful console commands:

```text
parse <csv-file>
import <csv-file> [--card <card-name>]
imports
report
report <csv-file> [csv-file...]
report month <yyyy-mm>
ai-key status
ai-key set
ai-key clear
ai-report [csv-file...]
review list
review set <transaction-id> <category-id> [--remember]
review confirm <transaction-id> [--remember]
```

## Packaging

Package the Windows app for sharing:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-windows.ps1
```

This creates a framework-dependent package at `dist/PeasantMoney-win-x64-framework-dependent.zip`. Friends need the .NET 10 Desktop Runtime installed. To bundle the runtime instead, run:

```powershell
powershell -ExecutionPolicy Bypass -File scripts/package-windows.ps1 -DeploymentMode SelfContained
```

Install Inno Setup and rerun the same script to also create a setup `.exe` in `dist/installer`.
