#!/usr/bin/env pwsh
<#
.SYNOPSIS
Queries open issues from dotnet/maui that need triage.

.DESCRIPTION
This script queries GitHub for open issues that:
- Have no milestone assigned
- Don't have blocking labels (needs-info, needs-repro, try-latest-version, etc.)
- Aren't Blazor issues (separate triage process)

.PARAMETER Platform
Filter by platform: "android", "ios", "windows", "maccatalyst", or "all" (default: "all")

.PARAMETER Area
Filter by area label (e.g., "collectionview", "shell")

.PARAMETER Limit
Maximum number of issues to return (default: 50)

.PARAMETER SortBy
Sort by: "created", "updated", "comments", "reactions" (default: "created")

.PARAMETER SortOrder
Sort order: "asc" or "desc" (default: "desc")

.PARAMETER MinAge
Minimum age in days (e.g., 7 for issues older than a week)

.PARAMETER MaxAge
Maximum age in days (e.g., 30 for issues newer than a month)

.PARAMETER OutputFormat
Output format: "table", "json", or "markdown" (default: "table")

.PARAMETER OutputFile
Optional file path to save results

.EXAMPLE
./QueryTriageIssues.ps1
# Returns up to 50 issues needing triage, sorted by creation date

.EXAMPLE
./QueryTriageIssues.ps1 -Platform android -Limit 20
# Returns up to 20 Android issues needing triage

.EXAMPLE
./QueryTriageIssues.ps1 -Area "collectionview" -SortBy reactions -OutputFormat markdown
# Returns CollectionView issues sorted by reactions, formatted as markdown

.EXAMPLE
./QueryTriageIssues.ps1 -MinAge 30 -OutputFile "old-issues.md" -OutputFormat markdown
# Saves issues older than 30 days to a markdown file
#>

param(
    [Parameter(Mandatory = $false)]
    [ValidateSet("android", "ios", "windows", "maccatalyst", "all")]
    [string]$Platform = "all",

    [Parameter(Mandatory = $false)]
    [string]$Area = "",

    [Parameter(Mandatory = $false)]
    [int]$Limit = 50,

    [Parameter(Mandatory = $false)]
    [ValidateSet("created", "updated", "comments", "reactions")]
    [string]$SortBy = "created",

    [Parameter(Mandatory = $false)]
    [ValidateSet("asc", "desc")]
    [string]$SortOrder = "desc",

    [Parameter(Mandatory = $false)]
    [int]$MinAge = 0,

    [Parameter(Mandatory = $false)]
    [int]$MaxAge = 0,

    [Parameter(Mandatory = $false)]
    [ValidateSet("table", "json", "markdown")]
    [string]$OutputFormat = "table",

    [Parameter(Mandatory = $false)]
    [string]$OutputFile = ""
)

$ErrorActionPreference = "Stop"

# Build the search query
# Base query: open issues, no milestone, excluding blocking labels
$baseQuery = "repo:dotnet/maui is:open is:issue no:milestone"

# Exclude blocking labels
$excludeLabels = @(
    "s/needs-info",
    "s/needs-repro",
    "area-blazor",
    "s/try-latest-version",
    "s/move-to-vs-feedback"
)

foreach ($label in $excludeLabels) {
    $baseQuery += " -label:`"$label`""
}

# Add platform filter
if ($Platform -ne "all") {
    $baseQuery += " label:`"platform/$Platform`""
}

# Add area filter
if ($Area -ne "") {
    $baseQuery += " label:`"area-$Area`""
}

# Add age filters
$today = Get-Date
if ($MinAge -gt 0) {
    $minDate = $today.AddDays(-$MinAge).ToString("yyyy-MM-dd")
    $baseQuery += " created:<$minDate"
}
if ($MaxAge -gt 0) {
    $maxDate = $today.AddDays(-$MaxAge).ToString("yyyy-MM-dd")
    $baseQuery += " created:>$maxDate"
}

Write-Host "Searching with query: $baseQuery" -ForegroundColor Cyan
Write-Host ""

# Execute the search
$searchArgs = @(
    "search", "issues",
    $baseQuery,
    "--limit", $Limit,
    "--sort", $SortBy,
    "--order", $SortOrder,
    "--json", "number,title,createdAt,updatedAt,comments,reactions,labels,url"
)

$result = & gh @searchArgs | ConvertFrom-Json

if ($result.Count -eq 0) {
    Write-Host "No issues found matching criteria." -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($result.Count) issues" -ForegroundColor Green
Write-Host ""

# Process results
$processedIssues = $result | ForEach-Object {
    $createdDate = [DateTime]::Parse($_.createdAt)
    $age = ($today - $createdDate).Days
    $ageString = if ($age -eq 0) { "today" } elseif ($age -eq 1) { "1 day" } else { "$age days" }

    # Extract platform labels
    $platformLabels = $_.labels | Where-Object { $_.name -like "platform/*" } | ForEach-Object { $_.name -replace "platform/", "" }
    $platformString = if ($platformLabels.Count -gt 0) { $platformLabels -join ", " } else { "unknown" }

    # Extract area labels
    $areaLabels = $_.labels | Where-Object { $_.name -like "area-*" } | ForEach-Object { $_.name -replace "area-", "" }
    $areaString = if ($areaLabels.Count -gt 0) { $areaLabels -join ", " } else { "none" }

    # Truncate title for display
    $displayTitle = if ($_.title.Length -gt 50) { $_.title.Substring(0, 47) + "..." } else { $_.title }

    [PSCustomObject]@{
        Number = $_.number
        Title = $displayTitle
        FullTitle = $_.title
        Age = $ageString
        AgeDays = $age
        Platform = $platformString
        Areas = $areaString
        Comments = $_.comments
        Reactions = $_.reactions.total_count
        URL = $_.url
    }
}

# Output based on format
function Format-Table-Output {
    param($issues)

    Write-Host ""
    Write-Host "Issues Needing Triage" -ForegroundColor Cyan
    Write-Host ("=" * 100)

    $issues | Format-Table -Property @(
        @{Label="Issue"; Expression={$_.Number}; Width=7},
        @{Label="Title"; Expression={$_.Title}; Width=50},
        @{Label="Age"; Expression={$_.Age}; Width=10},
        @{Label="Platform"; Expression={$_.Platform}; Width=12},
        @{Label="Comments"; Expression={$_.Comments}; Width=8}
    ) -AutoSize
}

function Format-Markdown-Output {
    param($issues)

    $output = @()
    $output += "# Issues Needing Triage"
    $output += ""
    $output += "Generated: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')"
    $output += "Query filters: Platform=$Platform, Area=$Area, MinAge=$MinAge, MaxAge=$MaxAge"
    $output += ""
    $output += "| Issue | Title | Age | Platform | Areas | Comments |"
    $output += "|-------|-------|-----|----------|-------|----------|"

    foreach ($issue in $issues) {
        $issueLink = "[#$($issue.Number)]($($issue.URL))"
        $output += "| $issueLink | $($issue.FullTitle -replace '\|', '\|') | $($issue.Age) | $($issue.Platform) | $($issue.Areas) | $($issue.Comments) |"
    }

    $output += ""
    $output += "---"
    $output += "Total: $($issues.Count) issues"

    return $output -join "`n"
}

function Format-Json-Output {
    param($issues)
    return $issues | ConvertTo-Json -Depth 10
}

# Generate output
switch ($OutputFormat) {
    "table" {
        Format-Table-Output -issues $processedIssues
        $outputContent = $null
    }
    "markdown" {
        $outputContent = Format-Markdown-Output -issues $processedIssues
        Write-Host $outputContent
    }
    "json" {
        $outputContent = Format-Json-Output -issues $processedIssues
        Write-Host $outputContent
    }
}

# Save to file if requested
if ($OutputFile -ne "" -and $outputContent) {
    $outputContent | Out-File -FilePath $OutputFile -Encoding UTF8
    Write-Host ""
    Write-Host "Results saved to: $OutputFile" -ForegroundColor Green
}

# Summary statistics
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Total issues: $($processedIssues.Count)"
Write-Host "  Average age: $([math]::Round(($processedIssues | Measure-Object -Property AgeDays -Average).Average, 1)) days"
Write-Host "  Platforms: $(($processedIssues | Group-Object Platform | ForEach-Object { "$($_.Name)=$($_.Count)" }) -join ', ')"

return $processedIssues
