#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Verifies that UI tests fail when the PR's fix is reverted.

.DESCRIPTION
    This script verifies that tests actually catch the issue by:
    1. Reverting the fix files to main branch
    2. Running tests WITHOUT the fix (should fail)
    3. Restoring the fix files
    4. Reporting whether tests correctly detect the issue

    This ensures the tests would catch the problem if the fix were missing.

.PARAMETER Platform
    Target platform: "android" or "ios"

.PARAMETER TestFilter
    Test filter to pass to dotnet test (e.g., "FullyQualifiedName~Issue12345")

.PARAMETER FixFiles
    Array of file paths (relative to repo root) that contain the fix.
    These files will be reverted to verify tests catch the issue.

.PARAMETER BaseBranch
    Branch to revert files from (default: "main")

.PARAMETER OutputDir
    Directory to store results (default: "CustomAgentLogsTmp/TestValidation")

.EXAMPLE
    ./verify-tests-fail.ps1 -Platform android -TestFilter "Issue20855" `
        -FixFiles @("src/Controls/src/Core/Handlers/Items/Android/Adapters/StructuredItemsViewAdapter.cs")

.EXAMPLE
    ./verify-tests-fail.ps1 -Platform ios -TestFilter "Issue12345" `
        -FixFiles @("src/Controls/src/Core/SomeFile.cs", "src/Controls/src/Core/OtherFile.cs")
#>

param(
    [Parameter(Mandatory = $true)]
    [ValidateSet("android", "ios")]
    [string]$Platform,

    [Parameter(Mandatory = $true)]
    [string]$TestFilter,

    [Parameter(Mandatory = $true)]
    [string[]]$FixFiles,

    [Parameter(Mandatory = $false)]
    [string]$BaseBranch = "main",

    [Parameter(Mandatory = $false)]
    [string]$OutputDir = "CustomAgentLogsTmp/TestValidation"
)

$ErrorActionPreference = "Stop"
$RepoRoot = git rev-parse --show-toplevel

# Create output directory
$OutputPath = Join-Path $RepoRoot $OutputDir
New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

$ValidationLog = Join-Path $OutputPath "verification-log.txt"
$WithoutFixLog = Join-Path $OutputPath "test-without-fix.log"

function Write-Log {
    param([string]$Message)
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logLine = "[$timestamp] $Message"
    Write-Host $logLine
    Add-Content -Path $ValidationLog -Value $logLine
}

function Get-TestResult {
    param([string]$LogFile)

    if (Test-Path $LogFile) {
        $content = Get-Content $LogFile -Raw
        if ($content -match "Failed:\s*(\d+)") {
            return @{ Passed = $false; FailCount = [int]$matches[1] }
        }
        if ($content -match "Passed:\s*(\d+)") {
            return @{ Passed = $true; PassCount = [int]$matches[1] }
        }
    }
    return @{ Passed = $false; Error = "Could not parse test results" }
}

# Initialize log
"" | Set-Content $ValidationLog
Write-Log "=========================================="
Write-Log "Verify Tests Fail Without Fix"
Write-Log "=========================================="
Write-Log "Platform: $Platform"
Write-Log "TestFilter: $TestFilter"
Write-Log "FixFiles: $($FixFiles -join ', ')"
Write-Log "BaseBranch: $BaseBranch"
Write-Log ""

# Verify fix files exist
Write-Log "Verifying fix files exist..."
foreach ($file in $FixFiles) {
    $fullPath = Join-Path $RepoRoot $file
    if (-not (Test-Path $fullPath)) {
        Write-Log "ERROR: Fix file not found: $file"
        exit 1
    }
    Write-Log "  ✓ $file"
}

# Store current state of fix files
Write-Log ""
Write-Log "Storing current state of fix files..."
$backupDir = Join-Path $OutputPath "fix-backup"
New-Item -ItemType Directory -Force -Path $backupDir | Out-Null

foreach ($file in $FixFiles) {
    $sourcePath = Join-Path $RepoRoot $file
    $destPath = Join-Path $backupDir (Split-Path $file -Leaf)
    Copy-Item $sourcePath $destPath -Force
    Write-Log "  Backed up: $file"
}

# Step 1: Revert fix files to base branch
Write-Log ""
Write-Log "=========================================="
Write-Log "STEP 1: Reverting fix files to $BaseBranch"
Write-Log "=========================================="

foreach ($file in $FixFiles) {
    Write-Log "  Reverting: $file"
    git checkout $BaseBranch -- $file 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Log "  Warning: Could not revert from $BaseBranch, trying origin/$BaseBranch"
        git checkout "origin/$BaseBranch" -- $file 2>&1 | Out-Null
    }
}

Write-Log "  Fix files reverted to $BaseBranch state"

# Step 2: Run tests WITHOUT fix
Write-Log ""
Write-Log "=========================================="
Write-Log "STEP 2: Running tests WITHOUT fix (should FAIL)"
Write-Log "=========================================="

# Use shared BuildAndRunHostApp.ps1 infrastructure
$buildScript = Join-Path $RepoRoot ".github/scripts/BuildAndRunHostApp.ps1"
& $buildScript -Platform $Platform -TestFilter $TestFilter 2>&1 | Tee-Object -FilePath $WithoutFixLog

$withoutFixResult = Get-TestResult -LogFile (Join-Path $RepoRoot "CustomAgentLogsTmp/UITests/test-output.log")

# Step 3: Restore fix files
Write-Log ""
Write-Log "=========================================="
Write-Log "STEP 3: Restoring fix files"
Write-Log "=========================================="

foreach ($file in $FixFiles) {
    $sourcePath = Join-Path $backupDir (Split-Path $file -Leaf)
    $destPath = Join-Path $RepoRoot $file
    Copy-Item $sourcePath $destPath -Force
    Write-Log "  Restored: $file"
}

# Step 4: Evaluate results
Write-Log ""
Write-Log "=========================================="
Write-Log "VERIFICATION RESULTS"
Write-Log "=========================================="

$verificationPassed = $false

if (-not $withoutFixResult.Passed) {
    Write-Log "✅ Tests FAILED without fix (expected - issue detected)"
    $verificationPassed = $true
} else {
    Write-Log "❌ Tests PASSED without fix (unexpected!)"
    Write-Log "   The tests don't detect the issue."
    Write-Log "   Either the tests are wrong or the fix files don't contain the actual fix."
}

Write-Log ""
Write-Log "Summary:"
Write-Log "  - Tests WITHOUT fix: $(if ($withoutFixResult.Passed) { 'PASS ❌ (should fail!)' } else { 'FAIL ✅ (expected)' })"

if ($verificationPassed) {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║              VERIFICATION PASSED ✅                       ║" -ForegroundColor Green
    Write-Host "╠═══════════════════════════════════════════════════════════╣" -ForegroundColor Green
    Write-Host "║  Tests correctly detect the issue:                        ║" -ForegroundColor Green
    Write-Host "║  - FAIL without fix (as expected)                         ║" -ForegroundColor Green
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║              VERIFICATION FAILED ❌                       ║" -ForegroundColor Red
    Write-Host "╠═══════════════════════════════════════════════════════════╣" -ForegroundColor Red
    Write-Host "║  Tests do NOT detect the issue:                           ║" -ForegroundColor Red
    Write-Host "║  - Tests pass even without the fix                        ║" -ForegroundColor Red
    Write-Host "║                                                           ║" -ForegroundColor Red
    Write-Host "║  Possible causes:                                         ║" -ForegroundColor Red
    Write-Host "║  1. Wrong fix files specified                             ║" -ForegroundColor Red
    Write-Host "║  2. Tests don't actually test the fixed behavior          ║" -ForegroundColor Red
    Write-Host "║  3. The issue was already fixed in base branch            ║" -ForegroundColor Red
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Red
    exit 1
}
