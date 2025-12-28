---
name: reproduce-issue
description: Guides through reproducing a GitHub issue by analyzing the issue report, creating a minimal test case, and verifying the bug exists. Use when starting work on a community-reported bug.
metadata:
  author: dotnet-maui
  version: "1.0"
compatibility: Requires git, dotnet CLI, and ability to run MAUI apps.
---

# Reproduce Issue

This skill guides you through reproducing a community-reported issue to verify it exists and understand its symptoms.

## When to Use

- "Reproduce issue #XXXXX"
- "Verify this bug exists"
- "Set up a test case for this issue"
- "Can you reproduce this problem?"
- Starting work on a new issue resolution

## Prerequisites

- Issue number and link to GitHub issue
- Access to dotnet CLI and MAUI tools
- Device/simulator for testing (if UI issue)

## Instructions

### Step 1: Analyze the Issue Report

```bash
# Fetch the issue details
gh issue view XXXXX

# Look for:
# - Clear description of the problem
# - Steps to reproduce
# - Expected vs actual behavior
# - Platform(s) affected
# - MAUI version
# - Sample code or repo link
```

**Key questions:**
1. What is the user-visible symptom?
2. What platforms are affected?
3. What version did it work in (if known)?
4. Is there a reproduction sample?

### Step 2: Check for Existing Reproduction

```bash
# Check if issue already has a test case
find src/Controls/tests/TestCases.HostApp/Issues -name "*$ISSUE_NUMBER*"

# Check if there's a linked sample repo in the issue
gh issue view XXXXX --json body | jq -r '.body' | grep -Eo 'https://github.com/[^/]+/[^/]+' | head -5
```

### Step 3: Create Minimal Reproduction

If no reproduction exists, create one:

**For UI issues - Use Sandbox app:**

```bash
# Use the sandbox-agent or create a test page
# Example: Create IssueXXXXX.xaml in Sandbox

cat > src/Controls/samples/Controls.Sample.Sandbox/IssueXXXXX.xaml << 'EOF'
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="Maui.Controls.Sample.IssueXXXXX"
             Title="Issue XXXXX">
    <VerticalStackLayout>
        <!-- Minimal repro code here -->
    </VerticalStackLayout>
</ContentPage>
EOF
```

**For API issues - Create unit test:**

```bash
# Create a unit test to reproduce
# Location: src/Controls/tests/Core.UnitTests/Issues/IssueXXXXX.cs
```

### Step 4: Run the Reproduction

```bash
# For Sandbox app
pwsh .github/scripts/BuildAndRunSandbox.ps1 -Platform ios

# For unit test
dotnet test src/Controls/tests/Core.UnitTests/Controls.Core.UnitTests.csproj --filter "IssueXXXXX"
```

**Document what you observe:**
- Does the issue reproduce?
- What is the exact symptom?
- Is it consistent or intermittent?
- Platform-specific or cross-platform?

### Step 5: Verify Issue Details

Compare your observations with the issue report:

| Aspect | Issue Report | Your Observation | Match? |
|--------|--------------|------------------|--------|
| Symptom | [from issue] | [what you see] | ✅/❌ |
| Platform | [from issue] | [what you tested] | ✅/❌ |
| Frequency | [from issue] | [consistent/intermittent] | ✅/❌ |

### Step 6: Document Reproduction

```markdown
## Reproduction Confirmed

**Issue**: #XXXXX
**Symptom**: [Brief description]
**Platforms affected**: [Android/iOS/Windows/Mac]
**Reproduced on**: [Your platform]

### Steps to reproduce:
1. [Step 1]
2. [Step 2]
3. [Step 3]

### Expected behavior:
[What should happen]

### Actual behavior:
[What actually happens]

### Test case location:
- [Path to test file or sandbox page]
```

## Output Format

```markdown
## Issue Reproduction Results

**Issue**: #XXXXX - [Title]
**Status**: ✅ Reproduced / ❌ Cannot reproduce / ⚠️ Partially reproduced

### Reproduction Details

**Platforms tested**: [List]
**MAUI version**: [Version from global.json]
**Test case**: [Location of test file]

### Observed Behavior

[Detailed description of what you observed]

### Differences from Report

[Any differences between issue report and your observations]

### Next Steps

- ✅ Issue reproduced → Proceed to root cause analysis
- ❌ Cannot reproduce → Request more info from reporter
- ⚠️ Partially reproduced → Test on additional platforms
```

## Common Issues

### Cannot Reproduce

**Possible reasons:**
- Issue is fixed in current version
- Platform-specific (test different platform)
- Specific device/configuration needed
- Missing setup steps

**Actions:**
- Check issue comments for additional context
- Try different MAUI version (if version-specific)
- Ask reporter for clarification

### Reproduction is Intermittent

**Possible reasons:**
- Race condition
- Timing-dependent
- Memory/resource dependent

**Actions:**
- Run multiple times
- Add delays/logging
- Test with debugger attached

### Reproduction Crashes

**Possible reasons:**
- Missing null checks
- Resource not found
- Platform API issue

**Actions:**
- Check device logs
- Add try-catch to narrow down
- Simplify reproduction case

## Tips

1. **Start simple** - Minimal reproduction is easier to debug
2. **Document everything** - Screenshots, logs, exact steps
3. **Test on reported platform first** - Then verify on others
4. **Compare with working version** - If issue says "worked in X.Y"
5. **Check related issues** - May provide additional context

## Related Skills

After reproducing:
- Use `root-cause-analysis` to find what's broken
- Use `assess-test-type` to determine test strategy
- Use `implement-fix` to create the solution
