<#
.SYNOPSIS
    Posts screenshot test results back to a GitHub PR comment.

.DESCRIPTION
    This script reads screenshot diff artifacts from all platforms,
    constructs a markdown summary, and posts it as a PR comment.
    It uses an HTML marker to update existing comments instead of creating duplicates.

    When run as part of the maui-pr-uitests pipeline, it collects all
    uitest-snapshot-results-* artifacts and summarizes them in one comment.

.PARAMETER PRNumber
    The PR number to post the comment to.

.PARAMETER ScreenshotsPath
    Path to the directory containing downloaded snapshot artifacts.

.PARAMETER BuildUrl
    URL to the Azure Pipeline build for linking.

.PARAMETER TestFilter
    Optional. The test category filter that was used.

.PARAMETER Platform
    Optional. The platform the tests ran on.

.PARAMETER TestResult
    Optional. Result of the test stage (Succeeded, Failed, etc.).

.EXAMPLE
    ./Post-ScreenshotsToPR.ps1 -PRNumber 12345 `
        -ScreenshotsPath "./snapshots" -BuildUrl "https://..."
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$PRNumber,

    [Parameter(Mandatory = $true)]
    [string]$ScreenshotsPath,

    [Parameter(Mandatory = $true)]
    [string]$BuildUrl,

    [Parameter(Mandatory = $false)]
    [string]$TestFilter = "",

    [Parameter(Mandatory = $false)]
    [string]$Platform = "",

    [Parameter(Mandatory = $false)]
    [string]$TestResult = "Unknown"
)

$ErrorActionPreference = "Stop"

$githubToken = $env:GITHUB_TOKEN
if (-not $githubToken) {
    Write-Error "GITHUB_TOKEN environment variable is required"
    exit 1
}

$owner = "dotnet"
$repo = "maui"
$marker = "<!-- screenshot-review -->"

# Determine status emoji
$statusEmoji = switch ($TestResult) {
    "Succeeded" { "✅" }
    "SucceededWithIssues" { "⚠️" }
    "Failed" { "❌" }
    "Canceled" { "🚫" }
    default { "🔍" }
}

# Collect all snapshot diff files across all platform artifact directories
$allScreenshots = @()
$allDiffs = @()
$platformSummaries = @()

if (Test-Path $ScreenshotsPath) {
    # Look for snapshots-diff in any subdirectory structure
    $allScreenshots = Get-ChildItem -Path $ScreenshotsPath -Filter "*.png" -Recurse |
        Where-Object { -not $_.Name.EndsWith("-diff.png") -and $_.FullName -like "*snapshots-diff*" }
    $allDiffs = Get-ChildItem -Path $ScreenshotsPath -Filter "*-diff.png" -Recurse |
        Where-Object { $_.FullName -like "*snapshots-diff*" }

    # Group by platform artifact directory
    $artifactDirs = Get-ChildItem -Path $ScreenshotsPath -Directory -ErrorAction SilentlyContinue
    foreach ($dir in $artifactDirs) {
        $dirScreenshots = Get-ChildItem -Path $dir.FullName -Filter "*.png" -Recurse |
            Where-Object { -not $_.Name.EndsWith("-diff.png") -and $_.FullName -like "*snapshots-diff*" }
        $dirDiffs = Get-ChildItem -Path $dir.FullName -Filter "*-diff.png" -Recurse |
            Where-Object { $_.FullName -like "*snapshots-diff*" }
        if ($dirScreenshots.Count -gt 0 -or $dirDiffs.Count -gt 0) {
            $platformSummaries += @{
                Name = $dir.Name
                Screenshots = $dirScreenshots.Count
                Diffs = $dirDiffs.Count
            }
        }
    }

    # If no subdirectories, check the root path directly
    if ($artifactDirs.Count -eq 0 -and ($allScreenshots.Count -gt 0 -or $allDiffs.Count -gt 0)) {
        $platformSummaries += @{
            Name = if ($Platform) { $Platform } else { "all" }
            Screenshots = $allScreenshots.Count
            Diffs = $allDiffs.Count
        }
    }
}

Write-Host "Found $($allScreenshots.Count) screenshot(s) and $($allDiffs.Count) diff(s) across $($platformSummaries.Count) platform(s)"

# If no diffs found, don't post a comment (nothing to report)
if ($allScreenshots.Count -eq 0 -and $allDiffs.Count -eq 0) {
    Write-Host "No snapshot diffs found. Skipping PR comment."
    exit 0
}

# Build the comment body
$body = @"
$marker
## 📸 Screenshot Diff Results

| | |
|---|---|
| **Status** | $statusEmoji $($allScreenshots.Count) screenshot(s), $($allDiffs.Count) diff(s) |
| **Build** | [View Pipeline]($BuildUrl) |
| **Artifacts** | [Download Screenshots]($($BuildUrl)&view=artifacts) |

"@

if ($TestFilter) { $body += "| **Filter** | ``$TestFilter`` |`n" }
if ($Platform) { $body += "| **Platform** | $Platform |`n" }

# Platform breakdown
if ($platformSummaries.Count -gt 1) {
    $body += "`n### Platform Breakdown`n`n"
    $body += "| Platform Artifact | Screenshots | Diffs |`n"
    $body += "|---|---|---|`n"
    foreach ($ps in $platformSummaries) {
        $diffIcon = if ($ps.Diffs -gt 0) { "⚠️" } else { "✅" }
        $body += "| ``$($ps.Name)`` | $($ps.Screenshots) | $diffIcon $($ps.Diffs) |`n"
    }
}

# Individual file listing
$body += "`n### Screenshots`n`n"
$body += "| File | Type |`n"
$body += "|------|------|`n"

foreach ($file in $allScreenshots) {
    $testName = [System.IO.Path]::GetFileNameWithoutExtension($file.Name)
    $hasDiff = $allDiffs | Where-Object { $_.Name -eq "$testName-diff.png" }
    if ($hasDiff) {
        $body += "| ``$testName`` | ⚠️ Diff from baseline |`n"
    }
    else {
        $body += "| ``$testName`` | 📷 New snapshot (no baseline) |`n"
    }
}

$body += @"

> 💡 Download the snapshot artifacts from the [pipeline build]($($BuildUrl)&view=artifacts) to view full-resolution images and diff overlays.
>
> **To update baselines:** copy new snapshots into ``src/Controls/tests/TestCases.Shared.Tests/snapshots/<platform>/``

---
_Posted by screenshot-review • [View build]($BuildUrl)_
"@

# Post or update the PR comment
$headers = @{
    "Accept"        = "application/vnd.github.v3+json"
    "Authorization" = "token $githubToken"
    "Content-Type"  = "application/json"
}

Write-Host "Fetching existing comments on PR #$PRNumber..."
$commentsUrl = "https://api.github.com/repos/$owner/$repo/issues/$PRNumber/comments?per_page=100"
$comments = Invoke-RestMethod -Uri $commentsUrl -Headers $headers -Method Get

$existingComment = $comments | Where-Object { $_.body -like "*$marker*" } | Select-Object -First 1

$payload = @{ body = $body } | ConvertTo-Json -Depth 10

if ($existingComment) {
    Write-Host "Updating existing screenshot review comment (ID: $($existingComment.id))..."
    $updateUrl = "https://api.github.com/repos/$owner/$repo/issues/comments/$($existingComment.id)"
    Invoke-RestMethod -Uri $updateUrl -Headers $headers -Method Patch -Body $payload | Out-Null
    Write-Host "✅ Comment updated"
}
else {
    Write-Host "Creating new screenshot review comment on PR #$PRNumber..."
    $createUrl = "https://api.github.com/repos/$owner/$repo/issues/$PRNumber/comments"
    Invoke-RestMethod -Uri $createUrl -Headers $headers -Method Post -Body $payload | Out-Null
    Write-Host "✅ Comment created"
}
