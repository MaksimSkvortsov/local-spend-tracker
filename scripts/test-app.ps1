param(
    [string]$BankOfAmericaCsv = "src/Spendnest.Infrastructure.Tests/Fixtures/Csv/bank-of-america.csv",
    [string]$CapitalOneCsv = "src/Spendnest.Infrastructure.Tests/Fixtures/Csv/capital-one.csv"
)

$ErrorActionPreference = "Stop"

$projectPath = "src/Spendnest.Console"
$tempDir = Join-Path ([System.IO.Path]::GetTempPath()) "spendnest-flow"
$mysteryCsv = Join-Path $tempDir "mystery-place.csv"
$oddCreditCsv = Join-Path $tempDir "odd-credit.csv"
$rememberedMysteryCsv = Join-Path $tempDir "remembered-mystery-place.csv"

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

function New-StatementFixtures {
    New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

    @"
Posted Date,Reference Number,Payee,Address,Amount
07/24/2026,900001,"MYSTERY PLACE","RIVERTON VA",-19.99
"@ | Set-Content -LiteralPath $mysteryCsv

    @"
Posted Date,Reference Number,Payee,Address,Amount
07/25/2026,900002,"ODD CREDIT","RIVERTON VA",-7.77
"@ | Set-Content -LiteralPath $oddCreditCsv

    @"
Posted Date,Reference Number,Payee,Address,Amount
07/26/2026,900003,"MYSTERY PLACE","RIVERTON VA",-11.11
"@ | Set-Content -LiteralPath $rememberedMysteryCsv
}

function Start-Spendnest {
    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = [System.Diagnostics.ProcessStartInfo]::new(
        "dotnet",
        "run --project $projectPath -- run")
    $process.StartInfo.WorkingDirectory = (Get-Location).Path
    $process.StartInfo.RedirectStandardInput = $true
    $process.StartInfo.RedirectStandardOutput = $true
    $process.StartInfo.UseShellExecute = $false
    $process.StartInfo.CreateNoWindow = $true

    [void]$process.Start()

    return $process
}

function Read-UntilPrompt {
    param([System.Diagnostics.Process]$Process)

    $buffer = [System.Text.StringBuilder]::new()
    $prompt = "spendnest> "

    while (-not $Process.HasExited) {
        $next = $Process.StandardOutput.Read()
        if ($next -lt 0) {
            break
        }

        [void]$buffer.Append([char]$next)

        if ($buffer.ToString().EndsWith($prompt, [StringComparison]::Ordinal)) {
            break
        }
    }

    return $buffer.ToString()
}

function Invoke-AppCommand {
    param(
        [System.Diagnostics.Process]$Process,
        [string]$Title,
        [string]$Command
    )

    Write-Host ""
    Write-Host "== $Title ==" -ForegroundColor Cyan
    Write-Host "spendnest> $Command" -ForegroundColor DarkGray

    $Process.StandardInput.WriteLine($Command)
    $output = Read-UntilPrompt $Process
    Write-Host $output.TrimEnd()

    if ($Process.HasExited -and $Process.ExitCode -ne 0) {
        throw "Spendnest exited with code $($Process.ExitCode)."
    }

    Wait-ForUser

    return $output
}

function Find-ReviewTransactionId {
    param(
        [string]$ReviewOutput,
        [string]$Description
    )

    foreach ($line in $ReviewOutput -split "`r?`n") {
        if (-not $line.Contains($Description)) {
            continue
        }

        $match = [regex]::Match($line, "[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}")
        if ($match.Success) {
            return $match.Value
        }
    }

    throw "Could not find a review transaction id for '$Description'."
}

Invoke-Step "Build solution" {
    dotnet build Spendnest.slnx --no-restore
}

Invoke-Step "Run tests" {
    dotnet test Spendnest.slnx --no-build
}

New-StatementFixtures
$app = Start-Spendnest

try {
    Write-Host ""
    Write-Host "== First launch ==" -ForegroundColor Cyan
    Write-Host (Read-UntilPrompt $app).TrimEnd()
    Wait-ForUser

    Invoke-AppCommand $app "Discover available commands" "help" | Out-Null

    Invoke-AppCommand $app "AI key status on first launch" "ai-key status" | Out-Null
    Invoke-AppCommand $app "Store an AI key for this app session" "ai-key set test-openai-key" | Out-Null
    Invoke-AppCommand $app "Verify AI key is stored" "ai-key status" | Out-Null
    Invoke-AppCommand $app "Clear the AI key" "ai-key clear" | Out-Null
    Invoke-AppCommand $app "Verify AI key is cleared" "ai-key status" | Out-Null

    Invoke-AppCommand $app "Preview a statement before importing" "parse `"$BankOfAmericaCsv`"" | Out-Null
    Invoke-AppCommand $app "Import first statement" "import `"$BankOfAmericaCsv`" --card `"Family Visa`"" | Out-Null
    Invoke-AppCommand $app "Import same statement again to show duplicate protection" "import `"$BankOfAmericaCsv`" --card `"Family Visa`"" | Out-Null
    Invoke-AppCommand $app "Run first category report" "report" | Out-Null
    Invoke-AppCommand $app "Categorize first imported transactions and save assignments" "categorize" | Out-Null
    Invoke-AppCommand $app "Check review queue after known merchants" "review list" | Out-Null
    Invoke-AppCommand $app "Run July report after categorization" "report month 2026-07" | Out-Null

    Invoke-AppCommand $app "Update data by importing another card statement" "import `"$CapitalOneCsv`" --card `"Travel Visa`"" | Out-Null
    Invoke-AppCommand $app "Categorize all current transactions" "categorize" | Out-Null
    Invoke-AppCommand $app "Run report for newly imported month" "report month 2025-12" | Out-Null
    Invoke-AppCommand $app "Run updated July report" "report month 2026-07" | Out-Null
    Invoke-AppCommand $app "Run all-time report across cards and months" "report" | Out-Null

    Invoke-AppCommand $app "Import unknown merchant for manual category set" "import `"$mysteryCsv`" --card `"Family Visa`"" | Out-Null
    Invoke-AppCommand $app "Import unknown credit for confirm flow" "import `"$oddCreditCsv`" --card `"Family Visa`"" | Out-Null
    Invoke-AppCommand $app "Categorize unknown transactions so they enter review" "categorize" | Out-Null
    $reviewOutput = Invoke-AppCommand $app "List review queue and capture generated transaction ids" "review list"

    $mysteryTransactionId = Find-ReviewTransactionId $reviewOutput "MYSTERY PLACE"
    $oddCreditTransactionId = Find-ReviewTransactionId $reviewOutput "ODD CREDIT"

    Invoke-AppCommand $app "Set Mystery Place to Entertainment and remember rule" "review set $mysteryTransactionId 5 --remember" | Out-Null
    Invoke-AppCommand $app "Confirm Odd Credit as its current category and remember rule" "review confirm $oddCreditTransactionId --remember" | Out-Null
    Invoke-AppCommand $app "Confirm review queue is clear" "review list" | Out-Null
    Invoke-AppCommand $app "Run July report after review updates" "report month 2026-07" | Out-Null

    Invoke-AppCommand $app "Import another Mystery Place transaction to prove remembered rule applies" "import `"$rememberedMysteryCsv`" --card `"Family Visa`"" | Out-Null
    Invoke-AppCommand $app "Categorize again after remembered rule" "categorize" | Out-Null
    Invoke-AppCommand $app "Review queue should remain clear after remembered rule" "review list" | Out-Null
    Invoke-AppCommand $app "Final July report after remembered rule" "report month 2026-07" | Out-Null

    Invoke-AppCommand $app "Exit Spendnest" "exit" | Out-Null
}
finally {
    if (-not $app.HasExited) {
        $app.StandardInput.WriteLine("exit")
        if (-not $app.WaitForExit(3000)) {
            $app.Kill()
        }
    }

    Remove-Item -LiteralPath $mysteryCsv -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $oddCreditCsv -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $rememberedMysteryCsv -Force -ErrorAction SilentlyContinue
}

Write-Host ""
Write-Host "Spendnest full user flow complete." -ForegroundColor Green
