#!/usr/bin/env pwsh
<#
.SYNOPSIS
Validates that UI tests correctly catch a regression by testing with and without the fix.

.DESCRIPTION
This script automates the test validation workflow:
1. Runs tests WITH the fix (should pass)
2. Reverts the fix files to main branch
3. Runs tests WITHOUT the fix (should fail)
4. Restores the fix files
5. Reports whether the tests correctly catch the regression

This ensures that the tests are actually testing the fix and would catch
a regression if the fix were reverted.

.PARAMETER Platform
Target platform: "android" or "ios"

.PARAMETER TestFilter
Test filter to pass to dotnet test (e.g., "FullyQualifiedName~Issue12345")

.PARAMETER FixFiles
Array of file paths (relative to repo root) that contain the fix.
These files will be reverted to test that the tests catch the regression.

.PARAMETER BaseBranch
Branch to revert files from (default: "main")

.PARAMETER OutputDir
Directory to store validation results (default: "CustomAgentLogsTmp/TestValidation")

.EXAMPLE
./validate-regression.ps1 -Platform android -TestFilter "Issue20855" `
  -FixFiles @("src/Controls/src/Core/Handlers/Items/Android/Adapters/StructuredItemsViewAdapter.cs")

.EXAMPLE
./validate-regression.ps1 -Platform ios -TestFilter "Issue12345" `
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

$ValidationLog = Join-Path $OutputPath "validation.log"
$WithFixLog = Join-Path $OutputPath "test-with-fix.log"
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
Write-Log "UI Test Validation - validate-ui-tests skill"
Write-Log "=========================================="
Write-Log "Platform: $Platform"
Write-Log "TestFilter: $TestFilter"
Write-Log "BaseBranch: $BaseBranch"
Write-Log "Fix files:"
foreach ($file in $FixFiles) {
    Write-Log "  - $file"
}

# Verify fix files exist
Write-Log ""
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

# Step 1: Run tests WITH fix
Write-Log ""
Write-Log "=========================================="
Write-Log "STEP 1: Running tests WITH fix (should PASS)"
Write-Log "=========================================="

# Use shared BuildAndRunHostApp.ps1 infrastructure
$buildScript = Join-Path $RepoRoot ".github/scripts/BuildAndRunHostApp.ps1"
& $buildScript -Platform $Platform -TestFilter $TestFilter 2>&1 | Tee-Object -FilePath $WithFixLog

$withFixResult = Get-TestResult -LogFile (Join-Path $RepoRoot "CustomAgentLogsTmp/UITests/test-output.log")
if ($withFixResult.Passed) {
    Write-Log "✅ Tests PASSED with fix (expected)"
    Write-Log "  Passed: $($withFixResult.PassCount)"
} else {
    Write-Log "❌ Tests FAILED with fix (unexpected!)"
    Write-Log "  Failed: $($withFixResult.FailCount)"
    Write-Log "  The fix doesn't make the tests pass. Check the fix implementation."
    
    # Restore fix files before exiting
    foreach ($file in $FixFiles) {
        $sourcePath = Join-Path $backupDir (Split-Path $file -Leaf)
        $destPath = Join-Path $RepoRoot $file
        Copy-Item $sourcePath $destPath -Force
    }
    
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║                   VALIDATION FAILED                       ║" -ForegroundColor Red
    Write-Host "╠═══════════════════════════════════════════════════════════╣" -ForegroundColor Red
    Write-Host "║          Tests don't pass WITH the fix.                  ║" -ForegroundColor Red
    Write-Host "║     The fix implementation needs to be corrected.        ║" -ForegroundColor Red
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Red
    exit 1
}

# Step 2: Revert fix files to base branch
Write-Log ""
Write-Log "=========================================="
Write-Log "STEP 2: Reverting fix files to $BaseBranch"
Write-Log "=========================================="

foreach ($file in $FixFiles) {
    $fullPath = Join-Path $RepoRoot $file
    git -C $RepoRoot checkout $BaseBranch -- $file 2>&1 | Out-Null
    Write-Log "  Reverted: $file"
}

# Step 3: Run tests WITHOUT fix
Write-Log ""
Write-Log "=========================================="
Write-Log "STEP 3: Running tests WITHOUT fix (should FAIL)"
Write-Log "=========================================="

& $buildScript -Platform $Platform -TestFilter $TestFilter 2>&1 | Tee-Object -FilePath $WithoutFixLog

$withoutFixResult = Get-TestResult -LogFile (Join-Path $RepoRoot "CustomAgentLogsTmp/UITests/test-output.log")

# Step 4: Restore fix files
Write-Log ""
Write-Log "=========================================="
Write-Log "STEP 4: Restoring fix files"
Write-Log "=========================================="

foreach ($file in $FixFiles) {
    $sourcePath = Join-Path $backupDir (Split-Path $file -Leaf)
    $destPath = Join-Path $RepoRoot $file
    Copy-Item $sourcePath $destPath -Force
    Write-Log "  Restored: $file"
}

# Step 5: Evaluate results
Write-Log ""
Write-Log "=========================================="
Write-Log "STEP 5: Evaluating results"
Write-Log "=========================================="

$validationPassed = $false

if (-not $withoutFixResult.Passed) {
    Write-Log "✅ Tests FAILED without fix (expected - regression detected)"
    Write-Log "  Failed: $($withoutFixResult.FailCount)"
    $validationPassed = $true
} else {
    Write-Log "❌ Tests PASSED without fix (unexpected!)"
    Write-Log "  Passed: $($withoutFixResult.PassCount)"
    Write-Log "  The tests don't catch the regression."
    Write-Log "  Either the tests are wrong or the fix files don't contain the actual fix."
}

Write-Log ""
Write-Log "Summary:"
Write-Log "  - Tests WITH fix: $(if ($withFixResult.Passed) { 'PASS ✅' } else { 'FAIL ❌' })"
Write-Log "  - Tests WITHOUT fix: $(if ($withoutFixResult.Passed) { 'PASS ❌ (should fail!)' } else { 'FAIL ✅ (expected)' })"

if ($validationPassed) {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Green
    Write-Host "║                   VALIDATION PASSED ✅                    ║" -ForegroundColor Green
    Write-Host "╠═══════════════════════════════════════════════════════════╣" -ForegroundColor Green
    Write-Host "║         Tests correctly catch the regression:             ║" -ForegroundColor Green
    Write-Host "║                     - PASS with fix                       ║" -ForegroundColor Green
    Write-Host "║                    - FAIL without fix                     ║" -ForegroundColor Green
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Red
    Write-Host "║                   VALIDATION FAILED ❌                    ║" -ForegroundColor Red
    Write-Host "╠═══════════════════════════════════════════════════════════╣" -ForegroundColor Red
    Write-Host "║         Tests do NOT catch the regression:                ║" -ForegroundColor Red
    Write-Host "║            - Tests pass even without the fix              ║" -ForegroundColor Red
    Write-Host "║                                                           ║" -ForegroundColor Red
    Write-Host "║                    Possible causes:                       ║" -ForegroundColor Red
    Write-Host "║              1. Wrong fix files specified                 ║" -ForegroundColor Red
    Write-Host "║    2. Tests don't actually test the fixed behavior        ║" -ForegroundColor Red
    Write-Host "║     3. The issue was already fixed in base branch         ║" -ForegroundColor Red
    Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Red
    exit 1
}
