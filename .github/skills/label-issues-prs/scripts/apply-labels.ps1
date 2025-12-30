#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Analyzes and applies appropriate labels to GitHub issues and pull requests.

.DESCRIPTION
    This script analyzes the content of GitHub issues or pull requests and 
    automatically applies appropriate labels based on:
    - Platform detection from content and file paths
    - Component/area detection from file paths
    - Issue type detection from content
    - Priority indicators

.PARAMETER Number
    The issue or PR number to label

.PARAMETER Type
    The type of item: "issue" or "pr"

.PARAMETER DryRun
    If specified, only analyze and show what labels would be applied without actually applying them

.PARAMETER Repository
    The repository in format "owner/repo". Defaults to current repository.

.EXAMPLE
    ./apply-labels.ps1 -Number 12345 -Type issue

.EXAMPLE
    ./apply-labels.ps1 -Number 67890 -Type pr -DryRun
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Number,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("issue", "pr")]
    [string]$Type,
    
    [Parameter(Mandatory = $false)]
    [switch]$DryRun,
    
    [Parameter(Mandatory = $false)]
    [string]$Repository = ""
)

# Check if gh CLI is installed
if (-not (Get-Command gh -ErrorAction SilentlyContinue)) {
    Write-Error "GitHub CLI (gh) is not installed. Please install it first."
    exit 1
}

# Construct repository argument
$repoArg = if ($Repository) { "--repo $Repository" } else { "" }

Write-Host "🔍 Analyzing $Type #$Number..." -ForegroundColor Cyan

# Fetch item details
$jsonFields = "title,body,labels"
if ($Type -eq "pr") {
    $jsonFields += ",files"
}

$itemJson = if ($Type -eq "issue") {
    gh issue view $Number --json $jsonFields $repoArg | ConvertFrom-Json
} else {
    gh pr view $Number --json $jsonFields $repoArg | ConvertFrom-Json
}

if (-not $itemJson) {
    Write-Error "Failed to fetch $Type #$Number"
    exit 1
}

$title = $itemJson.title
$body = $itemJson.body
$existingLabels = $itemJson.labels | ForEach-Object { $_.name }
$files = if ($Type -eq "pr") { $itemJson.files | ForEach-Object { $_.path } } else { @() }

Write-Host "📝 Title: $title" -ForegroundColor Yellow
Write-Host "📦 Existing labels: $($existingLabels -join ', ')" -ForegroundColor Gray

# Initialize label collections
$labelsToAdd = @()
$labelsToRemove = @()

# Platform Detection
$platforms = @()
$contentText = "$title $body $($files -join ' ')"

if ($contentText -match "(?i)android" -or $files -match "\.android\.cs|/Android/") {
    $platforms += "platform/android"
}
if ($contentText -match "(?i)ios" -or $files -match "\.ios\.cs|/iOS/") {
    $platforms += "platform/iOS"
}
if ($contentText -match "(?i)(mac\s*catalyst|maccatalyst)" -or $files -match "\.maccatalyst\.cs|/MacCatalyst/") {
    $platforms += "platform/macOS"
}
if ($contentText -match "(?i)windows" -or $files -match "\.windows\.cs|/Windows/") {
    $platforms += "platform/windows"
}

# Check if cross-platform
if ($platforms.Count -gt 1) {
    $labelsToAdd += "cross-platform"
} elseif ($platforms.Count -eq 1) {
    $labelsToAdd += $platforms[0]
}

# Area Detection from file paths
$areas = @()
foreach ($file in $files) {
    if ($file -match "src/Core/") { $areas += "area-core" }
    if ($file -match "src/Controls/") { $areas += "area-controls" }
    if ($file -match "src/Essentials/") { $areas += "area-essentials" }
    if ($file -match "src/BlazorWebView/") { $areas += "area-blazor" }
    if ($file -match "\.xaml(\.cs)?$") { $areas += "area-xaml" }
    if ($file -match "Handlers?/|Handler\.cs") { $areas += "area-handlers" }
    if ($file -match "Layout|LayoutManager") { $areas += "area-layout" }
}
$areas = $areas | Select-Object -Unique
$labelsToAdd += $areas

# Area Detection from content (if no files)
if ($areas.Count -eq 0) {
    if ($contentText -match "(?i)blazor") { $labelsToAdd += "area-blazor" }
    if ($contentText -match "(?i)essentials") { $labelsToAdd += "area-essentials" }
    if ($contentText -match "(?i)(handler|control)") { $labelsToAdd += "area-controls" }
    if ($contentText -match "(?i)xaml") { $labelsToAdd += "area-xaml" }
}

# Issue Type Detection
if ($body -match "(?i)### (steps to reproduce|reproduction)|### bug") {
    $labelsToAdd += "t/bug"
}
if ($title -match "(?i)\[proposal\]" -or $body -match "(?i)### (proposal|specification)") {
    $labelsToAdd += "t/proposal"
}
if ($title -match "(?i)\[feature\]|feature request" -or $body -match "(?i)### feature") {
    $labelsToAdd += "t/enhancement"
}

# Special Detection
if ($contentText -match "(?i)(crash|exception|throws)") {
    $labelsToAdd += "t/bug"
}
if ($contentText -match "(?i)regression") {
    $labelsToAdd += "t/regression"
}
if ($contentText -match "(?i)(memory leak|leaking)") {
    $labelsToAdd += "memory-leak"
}
if ($contentText -match "(?i)(performance|slow|lag)") {
    $labelsToAdd += "area-perf"
}
if ($contentText -match "(?i)breaking.*change|breaking.*api") {
    $labelsToAdd += "breaking-change"
}
if ($files -match "PublicAPI\.Unshipped\.txt") {
    $labelsToAdd += "area-publicapi"
}
if ($contentText -match "(?i)aot") {
    $labelsToAdd += "t/aot"
}

# Remove duplicates and filter out existing labels
$labelsToAdd = $labelsToAdd | Select-Object -Unique | Where-Object { $_ -notin $existingLabels }

# Check if needs-triage should be removed
if ("s/needs-triage" -in $existingLabels -and $labelsToAdd.Count -gt 0) {
    $labelsToRemove += "s/needs-triage"
}

# Display results
Write-Host "`n📊 Analysis Results:" -ForegroundColor Green
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray

if ($labelsToAdd.Count -eq 0 -and $labelsToRemove.Count -eq 0) {
    Write-Host "✅ No label changes needed - already properly labeled" -ForegroundColor Green
    exit 0
}

if ($labelsToAdd.Count -gt 0) {
    Write-Host "`n➕ Labels to Add:" -ForegroundColor Green
    foreach ($label in $labelsToAdd) {
        Write-Host "   • $label" -ForegroundColor Cyan
    }
}

if ($labelsToRemove.Count -gt 0) {
    Write-Host "`n➖ Labels to Remove:" -ForegroundColor Yellow
    foreach ($label in $labelsToRemove) {
        Write-Host "   • $label" -ForegroundColor Yellow
    }
}

# Apply labels if not dry run
if (-not $DryRun) {
    Write-Host ""
    $confirmation = Read-Host "Do you want to apply these labels? (y/N)"
    
    if ($confirmation -ne 'y' -and $confirmation -ne 'Y') {
        Write-Host "`n❌ Labeling cancelled by user" -ForegroundColor Yellow
        exit 0
    }
    
    Write-Host "`n🏷️  Applying labels..." -ForegroundColor Cyan
    
    # Add labels
    if ($labelsToAdd.Count -gt 0) {
        $labelList = $labelsToAdd -join ","
        if ($Type -eq "issue") {
            gh issue edit $Number --add-label $labelList $repoArg
        } else {
            gh pr edit $Number --add-label $labelList $repoArg
        }
        Write-Host "✅ Added labels successfully" -ForegroundColor Green
    }
    
    # Remove labels
    if ($labelsToRemove.Count -gt 0) {
        $labelList = $labelsToRemove -join ","
        if ($Type -eq "issue") {
            gh issue edit $Number --remove-label $labelList $repoArg
        } else {
            gh pr edit $Number --remove-label $labelList $repoArg
        }
        Write-Host "✅ Removed labels successfully" -ForegroundColor Green
    }
    
    Write-Host "`n🎉 Labeling complete!" -ForegroundColor Green
} else {
    Write-Host "`n⚠️  DRY RUN - No labels were actually applied" -ForegroundColor Yellow
    Write-Host "Run without -DryRun to apply these labels" -ForegroundColor Gray
}
