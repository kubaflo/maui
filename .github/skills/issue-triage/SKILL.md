---
name: issue-triage
description: Queries and triages open GitHub issues that need attention. Helps identify issues needing milestones, labels, or investigation.
metadata:
  author: dotnet-maui
  version: "1.0"
  compatibility: Requires GitHub CLI (gh) authenticated with access to dotnet/maui repository.
---

# Issue Triage Skill

This skill helps triage open GitHub issues in the dotnet/maui repository by:
1. Querying issues that need attention (no milestone, not blocked)
2. Filtering by platform, area, age, and other criteria
3. Prioritizing issues based on reactions, comments, or age
4. Providing actionable triage recommendations

## When to Use

- "Find issues to triage"
- "Show me old Android issues"
- "What CollectionView issues need attention?"
- "Find high-engagement issues without milestones"
- "Triage issues from the last week"

## Quick Start

```bash
# Get issues needing triage
pwsh .github/skills/issue-triage/scripts/query-issues.ps1

# Filter by platform
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -Platform android

# Filter by area
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -Area collectionview

# Sort by community engagement
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -SortBy reactions

# Export to markdown for review
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -OutputFormat markdown

# Advanced: Filter by age and save to file
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -MinAge 30 -SortOrder desc -OutputFile triage-list.md
```

## Script Parameters

| Parameter | Values | Default | Description |
|-----------|--------|---------|-------------|
| `-Platform` | android, ios, windows, maccatalyst, all | all | Filter by platform |
| `-Area` | Any area label (e.g., collectionview, shell) | "" | Filter by area |
| `-Limit` | 1-1000 | 50 | Maximum issues to return |
| `-SortBy` | created, updated, comments, reactions | created | Sort field |
| `-SortOrder` | asc, desc | desc | Sort direction |
| `-MinAge` | Days | 0 | Only issues older than N days |
| `-MaxAge` | Days | 0 | Only issues newer than N days |
| `-OutputFormat` | table, json, markdown | table | Output format |
| `-OutputFile` | File path | "" | Save results to file |

## Triage Workflow

### Step 1: Query Issues

```bash
# Start with a broad query
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -Limit 20 -OutputFormat markdown
```

### Step 2: Review Each Issue

For each issue, determine:

1. **Is it a valid bug?**
   - Has reproduction steps? → Proceed
   - Missing reproduction? → Add `s/needs-repro` label
   - Unclear problem? → Add `s/needs-info` label

2. **What platform is affected?**
   - Add appropriate `platform/*` label if missing

3. **What area does it affect?**
   - Add appropriate `area-*` label if missing

4. **What milestone should it target?**
   - Critical bug? → Current sprint milestone
   - Feature request? → Backlog or future milestone
   - Nice-to-have? → "Future" milestone

### Step 3: Take Action

```bash
# Add labels
gh issue edit ISSUE_NUMBER --add-label "platform/android,area-collectionview"

# Set milestone
gh issue edit ISSUE_NUMBER --milestone "9.0"

# Add to project (if applicable)
gh issue edit ISSUE_NUMBER --add-project "MAUI Backlog"

# Request more info
gh issue comment ISSUE_NUMBER --body "Thank you for the report! Could you please provide a minimal reproduction project?"
gh issue edit ISSUE_NUMBER --add-label "s/needs-repro"
```

## Common Triage Scenarios

### Find High-Priority Issues

```bash
# Issues with lots of reactions (community impact)
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -SortBy reactions -Limit 20

# Issues with lots of comments (discussion/complexity)
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -SortBy comments -Limit 20
```

### Find Stale Issues

```bash
# Issues older than 60 days without attention
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -MinAge 60 -SortBy created -SortOrder asc
```

### Platform-Specific Triage

```bash
# Android issues
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -Platform android -Limit 30

# iOS issues
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -Platform ios -Limit 30
```

### Area-Specific Triage

```bash
# CollectionView issues
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -Area collectionview

# Shell issues
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -Area shell

# Navigation issues
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -Area navigation
```

### Recent Issues (Fresh Triage)

```bash
# Issues from the last 7 days
pwsh .github/skills/issue-triage/scripts/query-issues.ps1 -MaxAge 7 -SortBy created -SortOrder desc
```

## Query Explanation

The script uses this base GitHub search query:

```
repo:dotnet/maui is:open is:issue no:milestone
-label:"s/needs-info"
-label:"s/needs-repro"
-label:"area-blazor"
-label:"s/try-latest-version"
-label:"s/move-to-vs-feedback"
```

**Why these exclusions?**

| Excluded Label | Reason |
|----------------|--------|
| `s/needs-info` | Already waiting for author response |
| `s/needs-repro` | Already waiting for reproduction |
| `area-blazor` | Separate triage process |
| `s/try-latest-version` | Waiting for author to test |
| `s/move-to-vs-feedback` | Should be filed in VS Feedback |

## Output Formats

### Table (Default)

```
Issue  Title                                          Age    Platform     Comments
-----  ---------------------------------------------  -----  -----------  --------
12345  CollectionView crashes on Android              5d     android      12
...
```

### Markdown

```markdown
# Issues Needing Triage

| Issue | Title | Age | Platform | Comments |
|-------|-------|-----|----------|----------|
| [#12345](https://...) | CollectionView crashes | 5d | android | 12 |
```

### JSON

```json
[
  {
    "number": 12345,
    "title": "...",
    "age": "5 days",
    "platform": "android",
    "comments": 12
  }
]
```

## Integration with Other Skills

After triaging, you can use other skills to investigate:

```bash
# Found an interesting issue? Reproduce it in Sandbox
# Use sandbox-agent to test

# Ready to fix? Use issue-resolver agent
# "Fix issue #12345"

# Need to review a PR for the issue? Use pr-reviewer
# "Review PR #67890"
```

## Best Practices

1. **Triage regularly** - Run triage queries weekly to prevent backlog growth
2. **Focus on recent issues first** - New issues need quick response
3. **Check high-engagement issues** - Community interest indicates priority
4. **Be consistent with labels** - Use standard labels for discoverability
5. **Communicate with authors** - If blocking on info, explain what's needed
6. **Close duplicates promptly** - Link to the original issue

## Triage Labels Reference

### Status Labels (`s/*`)

| Label | Meaning |
|-------|---------|
| `s/needs-info` | Waiting for author to provide more details |
| `s/needs-repro` | Waiting for reproduction project |
| `s/try-latest-version` | Author should test with latest version |
| `s/verified` | Issue has been reproduced |
| `s/triaged` | Issue has been reviewed and categorized |

### Platform Labels (`platform/*`)

- `platform/android`
- `platform/ios`
- `platform/windows`
- `platform/maccatalyst`

### Area Labels (`area-*`)

- `area-collectionview`
- `area-shell`
- `area-navigation`
- `area-controls`
- `area-layout`
- `area-graphics`
- `area-essentials`
