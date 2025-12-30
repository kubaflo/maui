#!/usr/bin/env pwsh

<#
.SYNOPSIS
    Batch labels multiple GitHub issues or pull requests.

.DESCRIPTION
    This script processes multiple GitHub issues or PRs and applies 
    appropriate labels to each after user confirmation.

.PARAMETER Count
    Number of latest items to process

.PARAMETER Type
    The type of items: "issues" or "prs"

.PARAMETER Filter
    Optional filter for items to process:
    - "unlabeled" - Items with no labels or minimal labels
    - "needs-triage" - Items with s/needs-triage label
    - "all" - All latest items (default)

.PARAMETER Repository
    The repository in format "owner/repo". Defaults to current repository.

.EXAMPLE
    ./batch-label.ps1 -Count 10 -Type issues

.EXAMPLE
    ./batch-label.ps1 -Count 20 -Type prs -Filter unlabeled
#>

param(
    [Parameter(Mandatory = $true)]
    [int]$Count,
    
    [Parameter(Mandatory = $true)]
    [ValidateSet("issues", "prs")]
    [string]$Type,
    
    [Parameter(Mandatory = $false)]
    [ValidateSet("unlabeled", "needs-triage", "all")]
    [string]$Filter = "all",
    
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

Write-Host "🔍 Fetching $Count latest $Type to label..." -ForegroundColor Cyan

# Build query based on filter
$query = ""
if ($Filter -eq "needs-triage") {
    $query = "label:s/needs-triage"
} elseif ($Filter -eq "unlabeled") {
    # Will filter after fetching
}

# Fetch items
$items = if ($Type -eq "issues") {
    if ($query) {
        gh issue list --limit $Count --search $query --json number,title,labels --state open $repoArg | ConvertFrom-Json
    } else {
        gh issue list --limit $Count --json number,title,labels --state open $repoArg | ConvertFrom-Json
    }
} else {
    if ($query) {
        gh pr list --limit $Count --search $query --json number,title,labels --state open $repoArg | ConvertFrom-Json
    } else {
        gh pr list --limit $Count --json number,title,labels --state open $repoArg | ConvertFrom-Json
    }
}

# Filter for unlabeled if requested
if ($Filter -eq "unlabeled") {
    $items = $items | Where-Object { 
        $labelCount = $_.labels.Count
        # Consider items with 0-2 labels as needing attention
        $labelCount -lt 3 -and 
        -not ($_.labels.name -contains "platform/android" -or 
              $_.labels.name -contains "platform/iOS" -or 
              $_.labels.name -contains "area-controls")
    }
}

if ($items.Count -eq 0) {
    Write-Host "✅ No items found matching criteria" -ForegroundColor Green
    exit 0
}

Write-Host "📋 Found $($items.Count) items to process" -ForegroundColor Yellow
Write-Host ""

# Get the script directory
$scriptDir = Split-Path -Parent $PSCommandItem.MyCommand.Path
$applyLabelsScript = Join-Path $scriptDir "apply-labels.ps1"

if (-not (Test-Path $applyLabelsScript)) {
    Write-Error "Could not find apply-labels.ps1 at $applyLabelsScript"
    exit 1
}

# Process each item
$processed = 0
$skipped = 0
$failed = 0

foreach ($item in $items) {
    $number = $item.number
    $title = $item.title
    $labelCount = $item.labels.Count
    
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
    Write-Host "📦 Processing #$number ($($processed + 1)/$($items.Count))" -ForegroundColor Cyan
    Write-Host "   $title" -ForegroundColor White
    Write-Host "   Current labels: $labelCount" -ForegroundColor Gray
    Write-Host ""
    
    # Run the apply-labels script
    $itemType = if ($Type -eq "issues") { "issue" } else { "pr" }
    
    try {
        & $applyLabelsScript -Number $number -Type $itemType -Repository $Repository
        
        if ($LASTEXITCODE -eq 0) {
            $processed++
        } else {
            $skipped++
        }
    } catch {
        Write-Host "❌ Failed to process #$number: $_" -ForegroundColor Red
        $failed++
    }
    
    Write-Host ""
}

# Summary
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Gray
Write-Host "`n📊 Batch Labeling Summary:" -ForegroundColor Green
Write-Host "   ✅ Processed: $processed" -ForegroundColor Green
Write-Host "   ⏭️  Skipped: $skipped" -ForegroundColor Yellow
Write-Host "   ❌ Failed: $failed" -ForegroundColor Red
Write-Host "   📦 Total: $($items.Count)" -ForegroundColor Cyan
Write-Host ""
