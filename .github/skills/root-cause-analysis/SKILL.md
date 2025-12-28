---
name: root-cause-analysis
description: Guides through analyzing git history, code changes, and execution flow to identify the root cause of a bug. Use after successfully reproducing an issue.
metadata:
  author: dotnet-maui
  version: "1.0"
compatibility: Requires git, ability to read C# code, and understanding of MAUI architecture.
---

# Root Cause Analysis

This skill guides you through finding the root cause of a bug by analyzing git history, code changes, and execution flow.

## When to Use

- "Find the root cause of issue #XXXXX"
- "What's causing this bug?"
- "When did this break?"
- "Analyze what changed"
- After successfully reproducing an issue

## Prerequisites

- Issue has been reproduced
- You understand the symptom
- Access to git history

## Instructions

### Step 1: Identify Affected Components

Based on the symptom, identify which components are involved:

```bash
# For UI rendering issues
# Look in: src/Controls/src/Core/Handlers/

# For binding issues
# Look in: src/Controls/src/Core/Binding/

# For layout issues
# Look in: src/Controls/src/Core/Layout/

# For platform-specific issues
# Look in platform folders: Android/, iOS/, Windows/
```

**Map symptom to component:**

| Symptom Type | Likely Component | Path Pattern |
|--------------|------------------|--------------|
| Element not rendering | Handler | `Handlers/*/` |
| Wrong size/position | Layout engine | `Layout/` |
| Binding not working | Binding system | `Binding/` |
| Event not firing | Handler or event routing | `Handlers/*/` |
| Platform-specific crash | Platform handler | `*.Android.cs`, `*.iOS.cs` |

### Step 2: Search Git History

```bash
# Find recent changes to the affected component
git log --oneline --all -20 -- path/to/Component.cs

# Search for issue-related keywords in commit messages
git log --oneline --all --grep="CollectionView" --since="6 months ago"

# Find when a specific file was modified
git log --follow --oneline -- path/to/File.cs

# See what changed in a specific commit
git show COMMIT_SHA
```

### Step 3: Identify Breaking Change

Look for:
- **PRs that modified the affected code**
- **Refactoring that changed behavior**
- **New features that had side effects**
- **Performance optimizations that broke edge cases**

```bash
# Find the PR that introduced a commit
gh pr list --search "SHA" --state merged

# See the full PR discussion
gh pr view PR_NUMBER

# Check if issue was introduced by a specific version
git tag --contains COMMIT_SHA
```

### Step 4: Compare Before/After

```bash
# Check out code before the breaking change
git checkout COMMIT_SHA~1 -- path/to/File.cs

# Test if issue still exists
# [Run your reproduction test]

# If issue is gone, the breaking commit is found
git checkout HEAD -- path/to/File.cs
```

### Step 5: Analyze the Breaking Change

```bash
# See exactly what changed
git diff GOOD_COMMIT..BAD_COMMIT -- path/to/File.cs

# Look for:
# - Removed null checks
# - Changed conditionals (==, !=, <, >)
# - Removed initialization
# - Changed method signatures
# - Reordered operations
```

**Common patterns:**

| Pattern | Impact |
|---------|--------|
| Removed null check | NullReferenceException |
| Changed `if (x == null)` to `if (x != null)` | Logic inversion |
| Removed initialization | Uninitialized state |
| Changed event timing | Race condition |
| Optimized away necessary work | Feature broken |

### Step 6: Understand Why It Broke

Ask these questions:

1. **What was the intent** of the breaking change?
   - Read the PR description
   - Check linked issues

2. **What assumption was made** that doesn't hold?
   - "Assumed X is never null"
   - "Assumed event fires before Y"
   - "Assumed only used in scenario Z"

3. **What edge case was missed?**
   - DataTemplateSelector vs DataTemplate
   - Grouped vs ungrouped CollectionView
   - Empty collections
   - Null values

### Step 7: Verify Your Theory

```bash
# Create a targeted test for your theory
# If you think it's a null reference issue:
# - Add null check and test
# If you think it's a timing issue:
# - Add delays and test
# If you think it's a missing initialization:
# - Add initialization and test
```

## Output Format

```markdown
## Root Cause Analysis

**Issue**: #XXXXX
**Component**: [Component name]
**File(s)**: [List of files involved]

### Timeline

| Date | Commit | Change | Impact |
|------|--------|--------|--------|
| [Date] | [SHA] | [Description] | Worked |
| [Date] | [SHA] | [Description] | ⚠️ Breaking change |
| Now | HEAD | - | Broken |

### Breaking Change

**Commit**: [SHA]
**PR**: #[PR_NUMBER]
**Author**: [@username]
**Intent**: [What the PR was trying to do]

**What changed**:
```diff
- old code
+ new code
```

### Root Cause

**Problem**: [One-sentence description]

**Why it broke**:
[Detailed explanation of the logic flaw, missed edge case, or incorrect assumption]

**Affected scenarios**:
1. [Scenario 1 - e.g., DataTemplateSelector with MeasureFirstItem]
2. [Scenario 2]

### Proposed Fix

**Approach**: [High-level fix strategy]

**Changes needed**:
1. [Change 1]
2. [Change 2]

**Files to modify**:
- [File 1]
- [File 2]
```

## Common Root Causes

### 1. Logic Inversion
```csharp
// Before (worked)
if (item != null)
    Process(item);

// After (broken)
if (item == null)  // ❌ Logic inverted
    Process(item);
```

### 2. Missing Null Check
```csharp
// Before (worked)
if (template != null && template.Id == 42)
    Optimize();

// After (broken)
if (template.Id == 42)  // ❌ Crashes if template is null
    Optimize();
```

### 3. Hardcoded Constant
```csharp
// Before (worked for all)
if (itemViewType != Header && itemViewType != Footer)
    Measure();

// After (broken for some)
if (itemViewType == TemplatedItem)  // ❌ Misses DataTemplateSelector IDs
    Measure();
```

### 4. Removed Initialization
```csharp
// Before (worked)
_isInitialized = true;
UpdateView();

// After (broken)
UpdateView();  // ❌ Removed initialization
```

### 5. Changed Execution Order
```csharp
// Before (worked)
A();  // Sets up state
B();  // Uses state

// After (broken)
B();  // ❌ Uses state before it's set
A();
```

## Tips

1. **Use git bisect** for complex cases
   ```bash
   git bisect start
   git bisect bad HEAD
   git bisect good v8.0.0
   # Test each commit until you find the culprit
   ```

2. **Check for related issues** - Others may have hit the same bug

3. **Read the PR discussion** - Often reveals the assumptions made

4. **Test the fix locally** before reviewing implementation

5. **Look for similar patterns** - If one handler has the bug, others might too

## Related Skills

After root cause analysis:
- Use `implement-fix` to create the solution
- Use `assess-test-type` to determine test strategy
- Use `independent-fix-analysis` if reviewing someone else's PR
