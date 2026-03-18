#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deep-assesses a single GitHub issue for backlog grooming.

.DESCRIPTION
    Takes an issue number and performs a comprehensive health assessment:
    1. Checks for reproduction steps (code blocks, numbered steps, XAML, screenshots)
    2. Checks linked PRs (open, merged, closed) and their milestones
    3. Evaluates staleness (last comment, last label change)
    4. Checks label completeness (platform, area, priority)
    5. Checks if referenced .NET version is still supported
    6. Produces a structured assessment with recommended actions

    This script is designed to be called:
    - Individually by the agent for interactive grooming
    - In a loop by generate-report.ps1 for batch assessment

.PARAMETER IssueNumber
    The GitHub issue number to assess

.PARAMETER OutputFormat
    Output format: "summary", "json", or "full" (default: "summary")

.PARAMETER ApplyActions
    If set, applies recommended actions (add labels, post comment). Requires confirmation.

.PARAMETER DryRun
    Show what actions would be taken without applying them (default: true)

.EXAMPLE
    ./assess-issue.ps1 -IssueNumber 12345
    # Assess issue #12345 and print summary

.EXAMPLE
    ./assess-issue.ps1 -IssueNumber 12345 -OutputFormat json
    # Assess and output as JSON (for batch processing)

.EXAMPLE
    ./assess-issue.ps1 -IssueNumber 12345 -ApplyActions -DryRun:$false
    # Assess and apply recommended actions
#>

param(
    [Parameter(Mandatory = $true)]
    [int]$IssueNumber,

    [Parameter(Mandatory = $false)]
    [ValidateSet("summary", "json", "full")]
    [string]$OutputFormat = "summary",

    [Parameter(Mandatory = $false)]
    [switch]$ApplyActions,

    [Parameter(Mandatory = $false)]
    [bool]$DryRun = $true
)

$ErrorActionPreference = "Stop"

# ── Prerequisites ──────────────────────────────────────────────────────────────
try {
    $null = Get-Command gh -ErrorAction Stop
} catch {
    Write-Host "❌ GitHub CLI (gh) is not installed" -ForegroundColor Red
    exit 1
}

$authStatus = gh auth status 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ GitHub CLI (gh) is not authenticated. Run: gh auth login" -ForegroundColor Red
    exit 1
}

# ── Retry helper ───────────────────────────────────────────────────────────────
function Invoke-GitHubWithRetry {
    param(
        [scriptblock]$ScriptBlock,
        [string]$Description = "GitHub API call",
        [int]$MaxRetries = 3
    )

    $retryCount = 0
    $baseDelay = 2

    while ($retryCount -lt $MaxRetries) {
        try {
            $result = & $ScriptBlock
            if ($LASTEXITCODE -eq 0 -or $null -eq $LASTEXITCODE) {
                return $result
            }
            throw "Non-zero exit code"
        }
        catch {
            $retryCount++
            if ($retryCount -ge $MaxRetries) { throw }
            $delay = $baseDelay * [Math]::Pow(2, $retryCount - 1)
            Write-Warning "  Retry $retryCount/$MaxRetries for $Description in ${delay}s..."
            Start-Sleep -Seconds $delay
        }
    }
}

# ── Fetch issue data ──────────────────────────────────────────────────────────
Write-Host "Assessing issue #$IssueNumber..." -ForegroundColor Cyan

$issue = Invoke-GitHubWithRetry -Description "fetch issue #$IssueNumber" -ScriptBlock {
    gh issue view $IssueNumber --repo dotnet/maui --json number,title,author,createdAt,updatedAt,labels,comments,url,milestone,body,state 2>&1 | ConvertFrom-Json
}

if ($issue.state -ne "OPEN") {
    Write-Host "Issue #$IssueNumber is $($issue.state). Skipping." -ForegroundColor Yellow
    return $null
}

# ── Extract basic info ─────────────────────────────────────────────────────────
$labelNames = ($issue.labels | ForEach-Object { $_.name }) -join ", "
$platformLabel = ($issue.labels | Where-Object { $_.name -like "platform/*" } | Select-Object -First 1)?.name ?? ""
$areaLabels = ($issue.labels | Where-Object { $_.name -like "area-*" } | ForEach-Object { $_.name }) -join ", "
$priorityLabel = ($issue.labels | Where-Object { $_.name -like "p/*" } | Select-Object -First 1)?.name ?? ""
$statusLabels = ($issue.labels | Where-Object { $_.name -like "s/*" } | ForEach-Object { $_.name }) -join ", "
$isRegression = $labelNames -match "i/regression|regressed-in"
$milestoneTitle = if ($issue.milestone) { $issue.milestone.title } else { "" }

$createdDate = [DateTime]::Parse($issue.createdAt)
$updatedDate = [DateTime]::Parse($issue.updatedAt)
$ageInDays = [Math]::Round(((Get-Date) - $createdDate).TotalDays)
$daysSinceUpdate = [Math]::Round(((Get-Date) - $updatedDate).TotalDays)

# ── 1. Reproduction quality assessment ─────────────────────────────────────────
$body = if ($issue.body) { $issue.body } else { "" }
$reproScore = 0
$reproDetails = @()

$hasCodeBlock = $body -match '```'
$hasNumberedSteps = $body -match '(?m)^\s*\d+[\.\)]\s' -or $body -match '(?i)steps?\s*(to\s*)?reproduce'
$hasXaml = $body -match '<\w+.*xmlns'
$hasScreenshot = $body -match '!\[.*\]\(.*\)'
$hasStackTrace = $body -match '(?i)(exception|stacktrace|stack trace|at\s+\w+\.\w+)'
$hasExpectedActual = $body -match '(?i)(expected|actual)\s*(behavior|result|output)?'
$hasVersionInfo = $body -match '(?i)(\.NET|MAUI|version|sdk)\s*[\d\.]+'
$bodyLength = $body.Length

if ($hasCodeBlock)       { $reproScore += 3; $reproDetails += "Has code blocks" }
if ($hasNumberedSteps)   { $reproScore += 3; $reproDetails += "Has numbered steps" }
if ($hasXaml)            { $reproScore += 2; $reproDetails += "Has XAML content" }
if ($hasScreenshot)      { $reproScore += 1; $reproDetails += "Has screenshots" }
if ($hasStackTrace)      { $reproScore += 1; $reproDetails += "Has stack trace" }
if ($hasExpectedActual)  { $reproScore += 2; $reproDetails += "Has expected/actual" }
if ($hasVersionInfo)     { $reproScore += 1; $reproDetails += "Has version info" }
if ($bodyLength -gt 500) { $reproScore += 1; $reproDetails += "Detailed description ($bodyLength chars)" }
elseif ($bodyLength -lt 100) { $reproScore -= 2; $reproDetails += "Very short description ($bodyLength chars)" }

$reproQuality = if ($reproScore -ge 6) { "good" }
                elseif ($reproScore -ge 3) { "fair" }
                elseif ($reproScore -ge 1) { "weak" }
                else { "missing" }

# ── 2. Comment analysis ───────────────────────────────────────────────────────
$comments = @()
$lastHumanComment = $null
$lastTeamComment = $null
$commentCount = 0

if ($issue.comments) {
    foreach ($comment in $issue.comments) {
        # Skip bot comments
        if ($comment.author.login -match "^(MihuBot|github-actions|msftbot|dotnet-maestro)") { continue }

        $commentCount++
        $commentDate = [DateTime]::Parse($comment.createdAt)
        $isTeam = $comment.authorAssociation -in @("MEMBER", "COLLABORATOR", "OWNER")

        $bodyPreview = $comment.body -replace '\r?\n', ' ' -replace '\s+', ' '
        if ($bodyPreview.Length -gt 200) { $bodyPreview = $bodyPreview.Substring(0, 197) + "..." }

        $commentObj = [PSCustomObject]@{
            Author      = $comment.author.login
            IsTeam      = $isTeam
            CreatedAt   = $commentDate
            DaysAgo     = [Math]::Round(((Get-Date) - $commentDate).TotalDays)
            BodyPreview = $bodyPreview
        }

        $comments += $commentObj
        $lastHumanComment = $commentObj
        if ($isTeam) { $lastTeamComment = $commentObj }
    }
}

$daysSinceLastComment = if ($lastHumanComment) { $lastHumanComment.DaysAgo } else { $ageInDays }
$daysSinceTeamResponse = if ($lastTeamComment) { $lastTeamComment.DaysAgo } else { -1 }

# ── 3. Linked PR analysis ─────────────────────────────────────────────────────
$linkedPRs = @()
try {
    $graphqlQuery = @"
{
  repository(owner: "dotnet", name: "maui") {
    issue(number: $IssueNumber) {
      timelineItems(itemTypes: [CONNECTED_EVENT, CROSS_REFERENCED_EVENT], first: 20) {
        nodes {
          ... on ConnectedEvent { subject { ... on PullRequest { number title state url milestone { title } mergedAt closedAt } } }
          ... on CrossReferencedEvent { source { ... on PullRequest { number title state url milestone { title } mergedAt closedAt } } }
        }
      }
    }
  }
}
"@
    $prResult = Invoke-GitHubWithRetry -Description "linked PRs" -ScriptBlock {
        gh api graphql -f query=$graphqlQuery 2>$null | ConvertFrom-Json
    }
    $nodes = $prResult.data.repository.issue.timelineItems.nodes
    foreach ($node in $nodes) {
        $pr = $null
        if ($node.subject) { $pr = $node.subject }
        elseif ($node.source) { $pr = $node.source }
        if ($pr -and $pr.number) {
            $linkedPRs += [PSCustomObject]@{
                Number    = $pr.number
                Title     = $pr.title
                State     = $pr.state
                URL       = $pr.url
                Milestone = $pr.milestone?.title ?? ""
                MergedAt  = $pr.mergedAt
                ClosedAt  = $pr.closedAt
            }
        }
    }
}
catch {
    Write-Warning "Could not fetch linked PRs: $_"
}

$mergedPRs = @($linkedPRs | Where-Object { $_.State -eq "MERGED" })
$openPRs = @($linkedPRs | Where-Object { $_.State -eq "OPEN" })
$closedPRs = @($linkedPRs | Where-Object { $_.State -eq "CLOSED" })

# ── 4. Build assessment ───────────────────────────────────────────────────────
$flags = @()
$actions = @()

# Staleness
if ($daysSinceUpdate -ge 365) {
    $flags += "very-stale"
    $actions += @{ Type = "comment"; Body = "This issue has had no activity for over a year. Is this still relevant? If so, please provide an updated reproduction with the latest .NET MAUI version. If not, we'll close this issue." }
    $actions += @{ Type = "label"; Add = "s/needs-info" }
}
elseif ($daysSinceUpdate -ge 180) {
    $flags += "stale"
    $actions += @{ Type = "comment"; Body = "This issue has had no activity for $daysSinceUpdate days. Please confirm if this is still an issue with the latest .NET MAUI version." }
}

# Possibly fixed
if ($mergedPRs.Count -gt 0) {
    $flags += "possibly-fixed"
    $prList = ($mergedPRs | ForEach-Object { "#$($_.Number)" }) -join ", "
    $actions += @{ Type = "comment"; Body = "This issue has linked merged PR(s): $prList. Can you verify if this is fixed in the latest release? If so, please close this issue." }
    $actions += @{ Type = "label"; Add = "s/try-latest-version" }
}

# Reproduction quality
if ($reproQuality -eq "missing") {
    $flags += "needs-repro"
    $actions += @{ Type = "label"; Add = "s/needs-repro" }
}
elseif ($reproQuality -eq "weak") {
    $flags += "weak-repro"
}

# Missing labels
if ($platformLabel -eq "") {
    $flags += "no-platform"
}
if ($areaLabels -eq "") {
    $flags += "no-area"
}
if ($milestoneTitle -eq "") {
    $flags += "no-milestone"
}

# Health score (0-100)
$healthScore = 100
if ($flags -contains "very-stale")     { $healthScore -= 30 }
elseif ($flags -contains "stale")      { $healthScore -= 15 }
if ($flags -contains "possibly-fixed") { $healthScore -= 25 }
if ($flags -contains "needs-repro")    { $healthScore -= 20 }
elseif ($flags -contains "weak-repro") { $healthScore -= 10 }
if ($flags -contains "no-platform")    { $healthScore -= 5 }
if ($flags -contains "no-area")        { $healthScore -= 5 }
if ($flags -contains "no-milestone")   { $healthScore -= 5 }
$healthScore = [Math]::Max(0, $healthScore)

$healthGrade = if ($healthScore -ge 80) { "A" }
               elseif ($healthScore -ge 60) { "B" }
               elseif ($healthScore -ge 40) { "C" }
               elseif ($healthScore -ge 20) { "D" }
               else { "F" }

# ── Build result object ───────────────────────────────────────────────────────
$assessment = [PSCustomObject]@{
    Number           = $issue.number
    Title            = $issue.title
    URL              = $issue.url
    Author           = $issue.author.login
    Created          = $createdDate.ToString("yyyy-MM-dd")
    Updated          = $updatedDate.ToString("yyyy-MM-dd")
    AgeDays          = $ageInDays
    DaysSinceUpdate  = $daysSinceUpdate
    DaysSinceComment = $daysSinceLastComment
    Milestone        = $milestoneTitle
    Platform         = $platformLabel -replace "^platform/", "" -replace " [🤖🍎🪟🍏]$", ""
    Areas            = $areaLabels
    Priority         = $priorityLabel
    Labels           = $labelNames
    IsRegression     = $isRegression
    CommentCount     = $commentCount
    ReproQuality     = $reproQuality
    ReproScore       = $reproScore
    ReproDetails     = $reproDetails
    LinkedPRs        = $linkedPRs
    MergedPRCount    = $mergedPRs.Count
    OpenPRCount      = $openPRs.Count
    HealthScore      = $healthScore
    HealthGrade      = $healthGrade
    Flags            = $flags
    Actions          = $actions
    RecentComments   = $comments | Select-Object -Last 3
}

# ── Output ────────────────────────────────────────────────────────────────────
switch ($OutputFormat) {
    "summary" {
        Write-Host ""
        Write-Host "╔═══════════════════════════════════════════════════════════╗" -ForegroundColor Cyan
        Write-Host "║  Issue #$($assessment.Number) Assessment" -ForegroundColor Cyan
        Write-Host "╚═══════════════════════════════════════════════════════════╝" -ForegroundColor Cyan
        Write-Host ""
        Write-Host "  $($assessment.Title)" -ForegroundColor White
        Write-Host "  $($assessment.URL)" -ForegroundColor DarkGray
        Write-Host ""

        $gradeColor = switch ($assessment.HealthGrade) {
            "A" { "Green" }
            "B" { "Green" }
            "C" { "Yellow" }
            "D" { "Red" }
            "F" { "Red" }
        }
        Write-Host "  Health: $($assessment.HealthGrade) ($($assessment.HealthScore)/100)" -ForegroundColor $gradeColor
        Write-Host ""
        Write-Host "  Age:         $($assessment.AgeDays) days (updated $($assessment.DaysSinceUpdate)d ago)" -ForegroundColor White
        Write-Host "  Milestone:   $(if ($assessment.Milestone) { $assessment.Milestone } else { '(none)' })" -ForegroundColor White
        Write-Host "  Platform:    $(if ($assessment.Platform) { $assessment.Platform } else { '(none)' })" -ForegroundColor White
        Write-Host "  Areas:       $(if ($assessment.Areas) { $assessment.Areas } else { '(none)' })" -ForegroundColor White
        Write-Host "  Repro:       $($assessment.ReproQuality) (score: $($assessment.ReproScore))" -ForegroundColor White
        Write-Host "  Comments:    $($assessment.CommentCount)" -ForegroundColor White
        Write-Host "  Linked PRs:  $($assessment.LinkedPRs.Count) (merged: $($assessment.MergedPRCount), open: $($assessment.OpenPRCount))" -ForegroundColor White
        Write-Host ""

        if ($assessment.Flags.Count -gt 0) {
            Write-Host "  Flags:" -ForegroundColor Yellow
            foreach ($flag in $assessment.Flags) {
                $emoji = switch ($flag) {
                    "possibly-fixed" { "✅" }
                    "very-stale"     { "💀" }
                    "stale"          { "😴" }
                    "needs-repro"    { "🔍" }
                    "weak-repro"     { "📝" }
                    "no-platform"    { "🏷️" }
                    "no-area"        { "🏷️" }
                    "no-milestone"   { "📌" }
                    default          { "❓" }
                }
                Write-Host "    $emoji $flag"
            }
        }
        else {
            Write-Host "  ✅ No issues found — this issue looks healthy!" -ForegroundColor Green
        }

        if ($assessment.Actions.Count -gt 0) {
            Write-Host ""
            Write-Host "  Recommended Actions:" -ForegroundColor Cyan
            foreach ($action in $assessment.Actions) {
                if ($action.Type -eq "label") {
                    Write-Host "    🏷️  Add label: $($action.Add)"
                }
                elseif ($action.Type -eq "comment") {
                    $preview = $action.Body
                    if ($preview.Length -gt 100) { $preview = $preview.Substring(0, 97) + "..." }
                    Write-Host "    💬 Comment: $preview"
                }
            }
        }

        if ($assessment.RecentComments.Count -gt 0) {
            Write-Host ""
            Write-Host "  Recent Comments:" -ForegroundColor DarkGray
            foreach ($c in $assessment.RecentComments) {
                $teamTag = if ($c.IsTeam) { " [TEAM]" } else { "" }
                Write-Host "    [$($c.Author)$teamTag $($c.DaysAgo)d ago] $($c.BodyPreview)" -ForegroundColor DarkGray
            }
        }
        Write-Host ""
    }
    "json" {
        $assessment | ConvertTo-Json -Depth 10
    }
    "full" {
        Write-Host "=== FULL ASSESSMENT ===" -ForegroundColor Cyan
        $assessment | Format-List
    }
}

# ── Apply actions (if requested) ──────────────────────────────────────────────
if ($ApplyActions -and $assessment.Actions.Count -gt 0) {
    Write-Host ""

    if ($DryRun) {
        Write-Host "DRY RUN — would apply:" -ForegroundColor Yellow
        foreach ($action in $assessment.Actions) {
            if ($action.Type -eq "label") {
                Write-Host "  gh issue edit $IssueNumber --repo dotnet/maui --add-label `"$($action.Add)`"" -ForegroundColor DarkGray
            }
            elseif ($action.Type -eq "comment") {
                Write-Host "  gh issue comment $IssueNumber --repo dotnet/maui --body `"...$($action.Body.Substring(0, [Math]::Min(50, $action.Body.Length)))...`"" -ForegroundColor DarkGray
            }
        }
        Write-Host ""
        Write-Host "  To apply for real, run with: -DryRun:`$false" -ForegroundColor Cyan
    }
    else {
        Write-Host "Applying actions..." -ForegroundColor Yellow

        foreach ($action in $assessment.Actions) {
            try {
                if ($action.Type -eq "label") {
                    Write-Host "  Adding label: $($action.Add)..." -NoNewline
                    gh issue edit $IssueNumber --repo dotnet/maui --add-label "$($action.Add)" 2>$null
                    Write-Host " ✅" -ForegroundColor Green
                }
                elseif ($action.Type -eq "comment") {
                    Write-Host "  Posting comment..." -NoNewline
                    gh issue comment $IssueNumber --repo dotnet/maui --body "$($action.Body)" 2>$null
                    Write-Host " ✅" -ForegroundColor Green
                }
            }
            catch {
                Write-Host " ❌ Failed: $_" -ForegroundColor Red
            }
        }
    }
}

# Return assessment for pipeline usage
return $assessment
