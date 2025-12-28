---
name: implement-fix
description: Guides through implementing a fix for an issue, including choosing the right approach, making minimal changes, and ensuring proper test coverage. Use after root cause analysis.
metadata:
  author: dotnet-maui
  version: "1.0"
compatibility: Requires understanding of C#, MAUI architecture, and git.
---

# Implement Fix

This skill guides you through implementing a fix after identifying the root cause of an issue.

## When to Use

- "Implement fix for issue #XXXXX"
- "Create a fix for this bug"
- "How should I fix this?"
- After completing root cause analysis

## Prerequisites

- Root cause has been identified
- You understand why it's broken
- You have a reproduction test case

## Instructions

### Step 1: Design the Fix

Consider multiple approaches:

1. **Minimal fix** - Smallest change that fixes the bug
2. **Defensive fix** - Adds guards to prevent similar issues
3. **Comprehensive fix** - Addresses underlying design issue

**Evaluation criteria:**

| Approach | Pros | Cons | When to Use |
|----------|------|------|-------------|
| **Minimal** | Low risk, easy to review | May miss edge cases | Hotfixes, unclear scope |
| **Defensive** | Prevents related bugs | Slightly more code | General bug fixes |
| **Comprehensive** | Better long-term | Higher risk, harder to review | Architectural issues |

**Default: Start with defensive approach**

### Step 2: Make the Code Change

```bash
# Create a branch
git checkout -b fix/issue-XXXXX

# Make your changes
# Edit the files...
```

**Best practices:**

✅ **Do:**
- Add null checks where needed
- Use clear variable names
- Add comments explaining non-obvious logic
- Handle edge cases (null, empty, zero)
- Maintain existing code style

❌ **Don't:**
- Change unrelated code
- Refactor while fixing
- Remove existing null checks
- Introduce new dependencies
- Break existing tests

**Example fix patterns:**

```csharp
// Pattern 1: Add null check
if (template != null && template.Id == TemplatedItem)
    Optimize();

// Pattern 2: Use defensive logic
if (itemViewType != Header && itemViewType != Footer && itemViewType != GroupHeader)
    Measure();

// Pattern 3: Initialize properly
EnsureInitialized();
UpdateView();

// Pattern 4: Guard against invalid state
if (!IsValid())
    return;
Process();
```

### Step 3: Create Test Case

Based on `assess-test-type` skill:

**For UI tests:**
```bash
# Create test page in TestCases.HostApp
# src/Controls/tests/TestCases.HostApp/Issues/IssueXXXXX.xaml
# src/Controls/tests/TestCases.HostApp/Issues/IssueXXXXX.xaml.cs

# Create NUnit test in TestCases.Shared.Tests
# src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/IssueXXXXX.cs
```

**For unit tests:**
```bash
# Create xUnit test
# src/Controls/tests/Core.UnitTests/Issues/IssueXXXXX.cs
```

**Test structure:**
```csharp
[Test]
[Category("Issue")]
public void IssueXXXXX()
{
    // Arrange - Set up the scenario
    
    // Act - Perform the action
    
    // Assert - Verify expected behavior
    Assert.That(actual, Is.EqualTo(expected), "Failure message");
}
```

### Step 4: Validate the Fix

Run tests in order:

```bash
# 1. Run new test WITH fix (should pass)
dotnet test --filter "IssueXXXXX"

# 2. Revert fix temporarily
git stash

# 3. Run new test WITHOUT fix (should fail)
dotnet test --filter "IssueXXXXX"

# 4. Restore fix
git stash pop

# 5. Run full test suite (ensure no regressions)
dotnet test src/Controls/tests/Core.UnitTests/
```

Or use validation skills:
```bash
# For UI tests
pwsh .github/skills/validate-ui-tests/scripts/validate-regression.ps1 \
  -Platform ios \
  -TestFilter "IssueXXXXX" \
  -FixFiles @("path/to/fixed/file.cs")

# For unit tests
pwsh .github/skills/validate-unit-tests/scripts/validate-regression.ps1 \
  -TestProject "path/to/test.csproj" \
  -TestFilter "IssueXXXXX" \
  -FixFiles @("path/to/fixed/file.cs")
```

### Step 5: Check for Similar Issues

```bash
# Search for similar patterns in the codebase
grep -rn "similar pattern" src/Controls/src/Core/

# Check if other handlers need the same fix
find src/Controls/src/Core/Handlers -name "*Handler*.cs" -exec grep -l "pattern" {} \;
```

### Step 6: Format and Clean Up

```bash
# Format code
dotnet format Microsoft.Maui.sln --no-restore --exclude Templates/src --exclude-diagnostics CA1822

# Check for auto-generated file changes (do NOT commit these)
git status

# Reset auto-generated files
git checkout HEAD -- cgmanifest.json templatestrings.json
```

### Step 7: Create Commit

```bash
git add .
git commit -m "Fix #XXXXX - [Brief description]

[Longer description of what was broken and how it's fixed]

Fixes #XXXXX"
```

**Commit message template:**
```
Fix #XXXXX - CollectionView items wrong height with DataTemplateSelector

The MeasureFirstItem optimization was checking for itemViewType == TemplatedItem,
but DataTemplateSelector assigns unique IDs (101+) to templates, not the constant
TemplatedItem value (42). This caused the optimization to be skipped incorrectly.

Changed the check to exclude header/footer types rather than checking for a
specific value, allowing all data item types to use the optimization.

Fixes #XXXXX
```

## Output Format

```markdown
## Fix Implementation Summary

**Issue**: #XXXXX - [Title]
**Root cause**: [Brief description]
**Fix approach**: [Minimal/Defensive/Comprehensive]

### Changes Made

**Files modified**:
1. [File 1] - [What changed]
2. [File 2] - [What changed]

**Code changes**:
- [Description of logic change]
- [Edge cases now handled]

**Lines changed**: ~[N] lines

### Test Coverage

**Test type**: UI Test / Unit Test
**Test location**: [Path to test file]
**Test validates**: [What the test checks]

✅ Test passes WITH fix
✅ Test fails WITHOUT fix (verified regression detection)

### Validation Results

- ✅ New test passes
- ✅ Existing tests pass
- ✅ No similar issues found in other handlers
- ✅ Code formatted
- ✅ Auto-generated files not included

### Ready for PR

**Branch**: `fix/issue-XXXXX`
**Commits**: 1 commit
**Status**: Ready for review
```

## Common Mistakes to Avoid

### 1. Too Broad Fix
```csharp
// ❌ Bad: Catches everything
try {
    DoSomething();
} catch { }

// ✅ Good: Specific fix
if (item != null)
    DoSomething(item);
```

### 2. Breaking Other Scenarios
```csharp
// ❌ Bad: Fixes one case, breaks another
if (template.Id == 42)
    Optimize();

// ✅ Good: Handles all cases
if (IsDataItem(template))
    Optimize();
```

### 3. Incomplete Fix
```csharp
// ❌ Bad: Only fixed Android
// src/Controls/src/Core/Handlers/Items/Android/Adapter.cs

// ✅ Good: Check all platforms
// Also check iOS: Items2/iOS/
```

### 4. No Test Coverage
```csharp
// ❌ Bad: Fixed bug but no test
// Bug can regress later

// ✅ Good: Fix + test
// Regression will be caught by CI
```

### 5. Committing Generated Files
```bash
# ❌ Bad: 
git add cgmanifest.json

# ✅ Good:
git checkout HEAD -- cgmanifest.json
```

## Tips

1. **Test on affected platform first** - Then verify cross-platform
2. **Keep changes minimal** - Easier to review and safer
3. **Document non-obvious logic** - Future maintainers will thank you
4. **Add defensive checks** - Better safe than sorry
5. **Verify test actually tests the fix** - Use validation skills

## Related Skills

After implementing:
- Use `validate-ui-tests` or `validate-unit-tests` to verify tests catch regression
- Use PR template for creating pull request
- Consider using `find-reviewable-pr` to see similar PRs for reference
