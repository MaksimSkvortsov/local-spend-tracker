param(
    [string]$BankOfAmericaCsv = "src/Spendnest.Infrastructure.Tests/Fixtures/Csv/bank-of-america.csv",
    [string]$CapitalOneCsv = "src/Spendnest.Infrastructure.Tests/Fixtures/Csv/capital-one.csv"
)

$ErrorActionPreference = "Stop"

$projectPath = "src/Spendnest.Console"
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "spendnest-flow"
$unknownCsv = Join-Path $tempDir "first-time-unknown.csv"

function Wait-ForUser {
    param([string]$Message = "Press any key to continue...")

    if (-not [Console]::IsInputRedirected) {
        Write-Host ""
        Write-Host $Message -ForegroundColor DarkGray
        [void][Console]::ReadKey($true)
    }
}

function Invoke-Step {
    param(
        [string]$Title,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "== $Title ==" -ForegroundColor Cyan
    & $Command
    Wait-ForUser
}

function Invoke-ConsoleSession {
    param(
        [string[]]$Commands
    )

    $script = ($Commands + "exit") -join [Environment]::NewLine
    $script | dotnet run --project $projectPath -- run
}

function New-UnknownStatement {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    @"
Posted Date,Reference Number,Payee,Address,Amount
07/24/2026,900001,"MYSTERY PLACE","RIVERTON VA",-19.99
"@ | Set-Content -LiteralPath $unknownCsv
}

Invoke-Step "Build solution" {
    dotnet build Spendnest.slnx --no-restore
}

Invoke-Step "Run tests" {
    dotnet test Spendnest.slnx --no-build
}

New-UnknownStatement

try {
    Invoke-Step "First launch: discover the app" {
        Invoke-ConsoleSession @(
            "help",
            "ai-key status"
        )
    }

    Invoke-Step "First statement: preview before importing" {
        Invoke-ConsoleSession @(
            "parse `"$BankOfAmericaCsv`""
        )
    }

    Invoke-Step "First statement: import, report, categorize, review queue" {
        Invoke-ConsoleSession @(
            "import `"$BankOfAmericaCsv`" --card `"Family Visa`"",
            "report",
            "categorize",
            "review list",
            "report month 2026-07"
        )
    }

    Invoke-Step "Update data: import another card statement and run new reports" {
        Invoke-ConsoleSession @(
            "import `"$BankOfAmericaCsv`" --card `"Family Visa`"",
            "categorize",
            "import `"$CapitalOneCsv`" --card `"Travel Visa`"",
            "categorize",
            "report month 2025-12",
            "report month 2026-07",
            "report"
        )
    }

    Invoke-Step "Review flow: create an unknown transaction and inspect review queue" {
        Invoke-ConsoleSession @(
            "import `"$BankOfAmericaCsv`" --card `"Family Visa`"",
            "import `"$unknownCsv`" --card `"Family Visa`"",
            "categorize",
            "review list",
            "report month 2026-07"
        )
    }

    Invoke-Step "Manual review command reference" {
        Write-Host "In the previous step, copy a transaction id from 'review list'."
        Write-Host "Then run this in an interactive Spendnest session:"
        Write-Host ""
        Write-Host "  review set <transaction-id> 5 --remember"
        Write-Host "  review list"
        Write-Host "  report month 2026-07"
        Write-Host ""
        Write-Host "Category id 5 is Entertainment in the MVP seed list."
        Write-Host "The app is still in-memory, so review updates currently last only inside the same app session."
    }
}
finally {
    Remove-Item -LiteralPath $unknownCsv -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Spendnest first-time and update flow complete." -ForegroundColor Green
