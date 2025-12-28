---
name: find-reviewable-pr
description: Finds open PRs in the dotnet/maui repository that are ready for review, filtering by platform, recency, complexity, and project board status.
metadata:
  author: dotnet-maui
  version: "1.0"
  compatibility: Requires GitHub CLI (gh) authenticated with access to dotnet/maui repository.
---

# Find Reviewable PR

This skill searches the dotnet/maui repository for open pull requests that are good candidates for review based on specified criteria.

## When to Use

- "Find a PR to review"
- "Find an easy Android PR to review"
- "Find a recent iOS PR that's ready for review"
- "What PRs are available for review?"
- "Find a PR from the last week"

## Default Criteria

When no specific criteria are given, the skill looks for PRs that:
1. Are **open** and **not draft**
2. Were **created in the last 7 days**
3. Are **marked as ready to review** in assigned projects (if any)
4. Are for **Android or iOS** platforms (most common request)
5. Are **relatively easy to review** (fewer files changed)

## Instructions

### Step 1: Search for Open PRs

```bash
# Search for recent, non-draft PRs
gh pr list --repo dotnet/maui --state open --limit 50 --json number,title,author,createdAt,isDraft
```

### Step 2: Filter by Platform (if specified)

```bash
# Filter by platform label
gh pr list --repo dotnet/maui --state open --label "platform/android" --limit 20
gh pr list --repo dotnet/maui --state open --label "platform/ios" --limit 20
```

### Step 3: Assess Complexity

Get the diff stats for each PR:

```bash
gh pr view PR_NUMBER --repo dotnet/maui --json additions,deletions,changedFiles
```

| Complexity | Criteria |
|------------|----------|
| **Easy** | ≤5 files changed, ≤200 additions, or test-only changes |
| **Medium** | 6-15 files changed, or 200-500 additions |
| **Complex** | >15 files changed, or >500 additions |

### Step 4: Check Project Board Status (Optional)

If user wants PRs marked "ready to review" in projects:

```bash
gh pr view PR_NUMBER --repo dotnet/maui --json projectItems
```

### Step 5: Rank by Recency and Engagement

Sort by:
- Creation date (newest first by default)
- Comment count (high engagement)
- Reactions (community interest)

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| Platform | Desired platform (android, ios, windows, any) | any |
| Recency | Max days old (e.g., 7 for last week) | 7 |
| Complexity | Desired complexity (easy, medium, complex, any) | easy |
| Project status | Require specific project board status | none |

## Example Queries and Responses

### Query: "Find an easy Android PR to review"

```markdown
## Reviewable PRs Found

| PR | Title | Platform | Complexity | Created |
|----|-------|----------|------------|---------|
| #33285 | Fixed Label Overlapped by Status Bar with SafeAreaEdges | Android | Medium | Dec 24 |
| #33295 | Add a log telling why the request is cancelled | Android | Easy | Dec 26 |

**Recommendation**: PR #33295 is a good choice - it's a simple logging addition with minimal code changes.
```

### Query: "Find a PR from the last week that's marked ready to review"

```markdown
## Reviewable PRs Found

Searched for: Open PRs, created after Dec 19, marked "Ready for Review" in projects

| PR | Title | Platform | Complexity | Project Status |
|----|-------|----------|------------|----------------|
| #33284 | Added test for Flyout CollectionView alignment | iOS | Easy | Ready for Review |

**Recommendation**: PR #33284 is test-only and marked ready for review.
```

## Output Format

Always provide:

```markdown
## Reviewable PRs Found

**Search criteria**: [Summary of filters applied]

| PR | Title | Platform | Complexity | Created |
|----|-------|----------|------------|---------|
| ... | ... | ... | ... | ... |

**Recommendation**: PR #XXXXX - [Brief reason why this is a good choice]

**Quick Stats**:
- Files changed: X
- Additions: +XXX
- Deletions: -XXX
```

## Tips

- **Test-only PRs** are often easier to review and good for getting familiar with the codebase
- **Community PRs** (labeled `community ✨`) may need more thorough review
- **Partner PRs** (labeled `partner/*`) often have business priority
- Check if the PR has **existing review comments** that need addressing
