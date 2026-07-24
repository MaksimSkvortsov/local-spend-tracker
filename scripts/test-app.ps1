param(
    [string]$BankOfAmericaCsv = "src/Spendnest.Infrastructure.Tests/Fixtures/Csv/bank-of-america.csv",
    [string]$CapitalOneCsv = "src/Spendnest.Infrastructure.Tests/Fixtures/Csv/capital-one.csv"
)

$ErrorActionPreference = "Stop"

function Invoke-Step {
    param(
        [string]$Title,
        [scriptblock]$Command
    )

    Write-Host ""
    Write-Host "== $Title ==" -ForegroundColor Cyan
    & $Command
    Write-Host ""
    if (-not [Console]::IsInputRedirected) {
        Write-Host "Press any key to continue..."
        [void][Console]::ReadKey($true)
    }
}

Invoke-Step "Build solution" {
    dotnet build Spendnest.slnx --no-restore
}

Invoke-Step "Run tests" {
    dotnet test Spendnest.slnx --no-build
}

Invoke-Step "Show console help" {
    dotnet run --project src/Spendnest.Console -- help
}

Invoke-Step "Parse Bank of America fixture" {
    dotnet run --project src/Spendnest.Console -- parse $BankOfAmericaCsv
}

Invoke-Step "Import Bank of America fixture to Family Visa" {
    dotnet run --project src/Spendnest.Console -- import $BankOfAmericaCsv --card "Family Visa"
}

Invoke-Step "Run category report from both fixture files" {
    dotnet run --project src/Spendnest.Console -- report $BankOfAmericaCsv $CapitalOneCsv
}

Invoke-Step "Run interactive workflow with monthly reports" {
    @"
parse $BankOfAmericaCsv
import $BankOfAmericaCsv --card "Family Visa"
report
import $CapitalOneCsv --card "Travel Visa"
report month 2026-07
report month 2025-12
exit
"@ | dotnet run --project src/Spendnest.Console -- run
}

Write-Host ""
Write-Host "Spendnest smoke test complete." -ForegroundColor Green
