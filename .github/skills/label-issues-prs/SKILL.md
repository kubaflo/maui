---
name: label-issues-prs
description: Applies appropriate labels to GitHub issues and pull requests based on content analysis, platform detection, and repository conventions
metadata:
  agentskillsVersion: "1.0.0"
  tags: 
    - github
    - labels
    - triage
    - automation
  compatibility:
    languages:
      - PowerShell
      - Bash
    tools:
      - gh
compatibility:
  models: "*"
  languages: "*"
---

# Label Issues and PRs Skill

Analyzes GitHub issues and pull requests to apply appropriate labels based on:
- Platform detection (Android, iOS, MacCatalyst, Windows)
- Component detection (Controls, Handlers, Blazor, etc.)
- Issue type (bug, enhancement, proposal)
- Priority indicators
- Area labels (XAML, AOT, etc.)

## When to Use This Skill

Use this skill when:
- **Issue or PR needs labeling** - After creating or reviewing content
- **Triage workflow** - Processing unlabeled or minimally labeled items
- **Label validation** - Verifying existing labels are correct
- **Batch labeling** - Processing multiple items systematically
- **User requests batch labeling** - "label the 10 latest issues", "apply labels to 20 recent PRs"

Do NOT use this skill when:
- Labels are already comprehensive and accurate
- User manually specified labels to apply
- Working on non-MAUI repositories with different conventions

## Prerequisites

- GitHub CLI (`gh`) installed and authenticated
- Repository access with label permissions
- Understanding of .NET MAUI architecture and components

## Instructions

### Step 1: Analyze Content

**For Issues:**
```bash
# Fetch issue details
gh issue view <issue-number> --json title,body,labels
```

**For Pull Requests:**
```bash
# Fetch PR details including files changed
gh pr view <pr-number> --json title,body,labels,files
```

**Key Analysis Points:**

1. **Platform Detection**
   - Look for keywords: "Android", "iOS", "Mac Catalyst", "MacCatalyst", "Windows"
   - Check file paths in PRs:
     - `*.android.cs`, `/Android/` → `platform/android`
     - `*.ios.cs`, `/iOS/` → `platform/iOS`
     - `*.maccatalyst.cs`, `/MacCatalyst/` → `platform/macOS`
     - `*.windows.cs`, `/Windows/` → `platform/windows`
   - Multiple platforms → `cross-platform` label

2. **Area Detection**
   - File paths and keywords:
     - `src/Core/` → `area-core`
     - `src/Controls/` → `area-controls`
     - `src/Essentials/` → `area-essentials`
     - `src/BlazorWebView/` → `area-blazor`
     - `*.xaml`, `*.xaml.cs` → `area-xaml`
     - Handlers/Layout → `area-layout`
     - AOT references → `t/aot`

3. **Issue Type**
   - Bug reports → `t/bug`
   - Feature requests → `t/enhancement`
   - Proposals (RFC style) → `t/proposal`
   - Questions → `s/needs-info` or close as discussion

4. **Priority Indicators**
   - Crashes, data loss → `p/1` (high priority)
   - Regressions → `t/regression`
   - Community vote count → Higher priority
   - Workarounds available → Lower priority

5. **Special Labels**
   - Breaking API changes → `breaking-change`
   - Public API changes → `area-publicapi`
   - Memory leaks → `memory-leak`
   - Performance issues → `area-perf`
   - Partner engagement → `partner/[partner-name]`

### Step 2: Apply Labels

**Using GitHub CLI:**
```bash
# Add labels to issue
gh issue edit <issue-number> --add-label "platform/android,area-controls,t/bug"

# Add labels to PR
gh pr edit <pr-number> --add-label "platform/iOS,area-core"

# Remove incorrect labels
gh issue edit <issue-number> --remove-label "incorrect-label"
```

**Using PowerShell Script:**
```powershell
# Example: Apply multiple labels programmatically
param(
    [string]$Number,
    [string]$Type = "issue",  # "issue" or "pr"
    [string[]]$Labels
)

$labelsArg = $Labels -join ","
if ($Type -eq "issue") {
    gh issue edit $Number --add-label $labelsArg
} else {
    gh pr edit $Number --add-label $labelsArg
}
```
Batch Labeling (Optional)

**For Multiple Items:**
```bash
# Get latest N issues that need labeling
gh issue list --limit 20 --json number,title,labels \
  --jq '.[] | select(.labels | length < 2) | .number'

# Process each one
for issue in $(gh issue list --limit 20 --json number --jq '.[].number'); do
  .github/skills/label-issues-prs/scripts/apply-labels.ps1 -Number $issue -Type issue
done
```

**Batch Script Workflow:**
1. Query issues/PRs that need labels (missing platform, area, or type labels)
2. For each item, run analysis and show proposed labels
3. Wait for user confirmation before applying each
4. Continue to next item

### Step 4: 
### Step 3: Validate Labels

**Check Label Consistency:**
- Platform labels should match affected platforms
- Area labels should match changed components
- Bug reports should have `t/bug`
- PRs fixing bugs should reference issue number

**Common Label Combinations:**
- Bug fix: `t/bug`, `area-[component]`, `platform/[platform]`
- Feature: `t/enhancement`, `area-[component]`, possibly `cross-platform`
- Breaking change: `breaking-change`, `area-publicapi`, `area-[component]`
- Regression: `t/bug`, `t/regression`, milestone set to affected version

## Output Format

**Report Applied Labels:**
```markdown
### Labels Applied

**Issue/PR #[number]**: [title]

**Added Labels:**
- `platform/android` - Affects Android platform
- `area-controls` - Related to Controls component
- `t/bug` - Bug report

**Removed Labels:**
- `s/needs-triage` - Item has been triaged

**Rationale:**
- File changes in `src/Controls/src/Core/Platform/Android/`
- Issue report describes button rendering issue on Android devices
- Contains reproduction steps and expected behavior
```

## Common Issues and Solutions

### Issue: Unclear Platform Scope
**Problem:** Issue mentions multiple platforms but unclear which are affected
**Solution:** 
- Ask reporter to clarify
- Add `s/needs-info` label
- If PR, check which platform files are changed

### Issue: Component Overlap
**Problem:** Changes span multiple areas (e.g., Core + Controls)
**Solution:**
- Apply both area labels
- Primary area should be most significant change
- Example: `area-controls,area-core`

### Issue: Priority Determination
**Problem:** Uncertain about priority level
**Solution:**
- Default to no priority label (team will assign)
- Use `p/1` only for crashes, data loss, or blocking issues
- Check community thumbs-up count for popularity

### Issue: Label Schema Changes
**Problem:** Repository label schema evolves
**Solution:**
- Check `.github/labels.yml` if it exists
- Review recent PRs for current conventions
- Ask team member if uncertain

## Tips for Success

1. **Check Existing Labels** - Review similar issues/PRs for patterns
2. **Platform-Specific Files** - File extensions are reliable indicators
3. **Be Conservative** - Better to under-label than mis-label
4. **Update as Needed** - Labels can be refined during triage
5. **Document Uncertainty** - Note when labels are best-guess
6. **Batch Operations** - Process related items together for consistency

## Related Skills

- **issue-triage** - Finding issues that need labeling
- **find-reviewable-pr** - Identifying PRs ready for review (uses labels)

## Script Location

**Single Item Labeling:**
`.github/skills/label-issues-prs/scripts/apply-labels.ps1`

**Batch Labeling:**
`.github/skills/label-issues-prs/scripts/batch-label.ps1`

### Batch Labeling Examples

```bash
# Label 10 latest issues
.github/skills/label-issues-prs/scripts/batch-label.ps1 -Count 10 -Type issues

# Label 20 latest PRs that are unlabeled
.github/skills/label-issues-prs/scripts/batch-label.ps1 -Count 20 -Type prs -Filter unlabeled

# Label issues with needs-triage label
.github/skills/label-issues-prs/scripts/batch-label.ps1 -Count 15 -Type issues -Filter needs-triage
```

These scripts analyze content and apply labels programmatically, with user confirmation for each item.
