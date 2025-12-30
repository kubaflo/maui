---
name: verify-tests-fail-without-fix
description: Verifies that UI tests fail when the PR's fix is reverted, proving the tests actually catch the issue. Use to validate test quality.
metadata:
  author: dotnet-maui
  version: "3.0"
compatibility: Requires iOS simulator or Android emulator, PowerShell, and BuildAndRunHostApp.ps1 script.
---

# Verify Tests Fail Without Fix

This skill verifies that UI tests in a PR actually catch the issue by:
1. Reverting the fix files to main branch
2. Running the tests WITHOUT the fix
3. Confirming the tests FAIL (proving they detect the problem)

## When to Use

- "Verify tests fail without the fix"
- "Check if tests catch the issue"
- "Prove the tests actually detect the bug"
- After confirming tests pass WITH the fix

## Prerequisites

- PR is checked out
- Fix files and test files are identified
- `assess-test-type` has confirmed these should be UI tests
- iOS simulator or Android emulator available

## Dependencies

This skill uses the shared infrastructure script:
- `.github/scripts/BuildAndRunHostApp.ps1` - Test runner for UI tests across platforms

The validation script in this skill calls BuildAndRunHostApp.ps1 to execute tests.

## Quick Method: Use the Skill Script (Recommended)

The fastest way to verify tests fail without the fix:

```bash
pwsh .github/skills/verify-tests-fail-without-fix/scripts/verify-tests-fail.ps1 \
    -Platform android \
    -TestFilter "Issue20855" \
    -FixFiles @("src/Controls/src/Core/Handlers/Items/Android/Adapters/StructuredItemsViewAdapter.cs")
```

The script will:
1. ✅ Revert fix files to main branch
2. ✅ Run tests WITHOUT fix (verify they fail)
3. ✅ Restore fix files
4. ✅ Report whether tests correctly detect the issue

**Output:**
```
╔═══════════════════════════════════════════════════════════╗
║              VERIFICATION PASSED ✅                       ║
╠═══════════════════════════════════════════════════════════╣
║  Tests correctly detect the issue:                        ║
║  - FAIL without fix (as expected)                         ║
╚═══════════════════════════════════════════════════════════╝
```

---

## Manual Method (Step-by-Step)

If you need more control or want to debug issues:

### Step 1: Identify Fix Files

```bash
# Find the fix files (non-test code changes)
git diff main --name-only | grep -v "Test"
```

### Step 2: Identify the Test Name

```bash
# Find the issue number from test files
git diff main --name-only | grep -oE "Issue[0-9]+" | head -1
```

### Step 3: Revert the Fix

```bash
# Revert only the fix files (not the test files)
git checkout main -- path/to/fix/File1.cs path/to/fix/File2.cs
```

### Step 4: Run Tests WITHOUT Fix

```bash
# Kill any existing Appium processes
lsof -i :4723 | grep LISTEN | awk '{print $2}' | xargs kill -9 2>/dev/null

# Run the tests
pwsh .github/scripts/BuildAndRunHostApp.ps1 -Platform ios -TestFilter "IssueXXXXX" 2>&1 | tee CustomAgentLogsTmp/UITests/test-without-fix.log
```

**Expected**: Tests should FAIL with a meaningful assertion error

### Step 5: Verify Failure Reason

```bash
# Check the test output
grep -A20 "Failed\|Assert\|Error" CustomAgentLogsTmp/UITests/test-without-fix.log
```

**Good failure examples:**
- ✅ `Expected: "Choice 2", But was: "None"` - Correctly tests binding
- ✅ `Element 'ExpectedElement' should be visible` - Correctly tests rendering
- ✅ `Assert.That(rect.Height, Is.GreaterThan(0))` - Correctly tests layout

**Bad failure examples:**
- ❌ `Element not found: TestButton` - Test design issue, not the bug
- ❌ `App crashed` - Unrelated problem (check device logs)
- ❌ `Timeout waiting for element` - May be environment issue

### Step 6: Restore the Fix

```bash
git checkout HEAD -- path/to/fix/File1.cs path/to/fix/File2.cs
```

## Output Format

```markdown
## Verification Results

**Platform**: iOS / Android
**Test Filter**: `IssueXXXXX`

| Scenario | Expected | Actual | Status |
|----------|----------|--------|--------|
| Without fix | FAIL | PASS/FAIL | ✅/❌ |

### Failure Analysis

**Assertion Error**:
```
[Quote the actual assertion failure message]
```

**Does failure match the issue?**
- ✅ Yes - Failure directly relates to the reported bug
- OR
- ❌ No - Failure is unrelated because [reason]

### Conclusion

- ✅ Tests correctly detect the issue without the fix
- OR
- ⚠️ Tests need improvement because [reason]
```

## Common Issues

### Tests Pass Without Fix (BAD)

This means the tests don't actually detect the issue.

**Possible causes:**
- Test checking wrong element/property
- Test has race condition (passes sometimes)
- Issue only occurs on specific platform

**Solutions:**
- Review test assertions and element locators
- Run test multiple times
- Test on the affected platform

### App Crashes

**Possible causes:**
- Duplicate issue numbers in `[Issue]` attributes
- XAML parse error
- Missing event handler

**Solutions:**
```bash
# Check for duplicate issue numbers
grep -r "IssueTracker.Github, XXXXX" src/Controls/tests/

# Check device logs for exception
grep -i "exception" CustomAgentLogsTmp/UITests/ios-device.log | head -10
```

### Element Not Found

**Possible causes:**
- App crashed before element rendered
- Wrong AutomationId
- Element not visible (scrolling needed)

**Solutions:**
- Check device logs for crashes first
- Verify AutomationId matches between XAML and test
- Check if element requires scrolling to be visible

## Platform Selection

```bash
# iOS (faster startup, recommended for most tests)
pwsh .github/scripts/BuildAndRunHostApp.ps1 -Platform ios -TestFilter "IssueXXXXX"

# Android (if iOS-specific issue or need Android validation)
pwsh .github/scripts/BuildAndRunHostApp.ps1 -Platform android -TestFilter "IssueXXXXX"

# MacCatalyst
pwsh .github/scripts/BuildAndRunHostApp.ps1 -Platform maccatalyst -TestFilter "IssueXXXXX"
```
