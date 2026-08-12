param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$RuntimeIdentifier = "win-x64",

    [string]$Configuration = "Release",

    [ValidateSet("FrameworkDependent", "SelfContained")]
    [string]$DeploymentMode = "FrameworkDependent"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot "src/Spendnest.App/Spendnest.App.csproj"
$targetFramework = "net10.0-windows10.0.19041.0"
$distDir = Join-Path $repoRoot "dist"
$modeSuffix = if ($DeploymentMode -eq "FrameworkDependent") { "framework-dependent" } else { "self-contained" }
$publishDir = Join-Path $distDir "publish/PeasantMoney-$RuntimeIdentifier-$modeSuffix"
$zipPath = Join-Path $distDir "PeasantMoney-$RuntimeIdentifier-$modeSuffix.zip"
$innoScript = Join-Path $repoRoot "installer/Spendnest.iss"
$selfContained = $DeploymentMode -eq "SelfContained"

New-Item -ItemType Directory -Path $distDir -Force | Out-Null

if (Test-Path -LiteralPath $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

Write-Host "Publishing Spendnest for $RuntimeIdentifier ($DeploymentMode)..." -ForegroundColor Cyan
dotnet publish $projectPath `
    -c $Configuration `
    -f $targetFramework `
    -p:RuntimeIdentifier=$RuntimeIdentifier `
    -p:WindowsPackageType=None `
    -p:SelfContained=$selfContained `
    -p:PublishDir="$publishDir\"

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

Write-Host "Creating $zipPath..." -ForegroundColor Cyan
Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if ($iscc) {
    Write-Host "Building Setup.exe with Inno Setup..." -ForegroundColor Cyan
    & $iscc.Source `
        "/DRuntimeIdentifier=`"$RuntimeIdentifier`"" `
        "/DConfiguration=`"$Configuration`"" `
        "/DDeploymentMode=`"$DeploymentMode`"" `
        "/DModeSuffix=`"$modeSuffix`"" `
        "/DPublishDir=`"$publishDir`"" `
        $innoScript
} else {
    Write-Host "Inno Setup was not found, so only the zip was created." -ForegroundColor Yellow
    Write-Host "Install Inno Setup and rerun this script to create dist/installer/PeasantMoneySetup-$RuntimeIdentifier-$modeSuffix.exe."
}

Write-Host ""
Write-Host "Package ready:" -ForegroundColor Green
Write-Host "  $zipPath"
