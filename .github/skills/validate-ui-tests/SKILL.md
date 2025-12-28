---
name: validate-ui-tests
description: Validates that UI tests correctly fail without a fix and pass with a fix. Use after assess-test-type confirms UI tests are appropriate.
metadata:
  author: dotnet-maui
  version: "2.0"
compatibility: Requires iOS simulator or Android emulator, PowerShell, and BuildAndRunHostApp.ps1 script.
---

# Validate UI Tests

This skill validates that UI tests in a PR correctly catch regressions by:
1. Running tests WITH the fix (should pass)
2. Reverting the fix and running tests WITHOUT the fix (should fail)
3. Confirming the failure reason matches the expected issue

## When to Use

- "Validate the UI tests"
- "Check if UI tests catch the regression"
- "Verify UI tests fail without the fix"
- After `assess-test-type` confirms UI tests are appropriate

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

The fastest way to validate UI tests is to use this skill's script:

```bash
# Validate UI tests catch the regression
pwsh .github/skills/validate-ui-tests/scripts/validate-regression.ps1 \
  -Platform android \
  -TestFilter "Issue20855" \
  -FixFiles @("src/Controls/src/Core/Handlers/Items/Android/Adapters/StructuredItemsViewAdapter.cs")
```

The script will:
1. ✅ Run tests WITH fix (verify they pass)
2. ✅ Automatically revert fix files to main
3. ✅ Run tests WITHOUT fix (verify they fail)
4. ✅ Restore fix files
5. ✅ Report whether validation passed or failed

**Output:**
```
╔═══════════════════════════════════════════════════════════╗
║                   VALIDATION PASSED ✅                    ║
╠═══════════════════════════════════════════════════════════╣
║         Tests correctly catch the regression:             ║
║                     - PASS with fix                       ║
║                    - FAIL without fix                     ║
╚═══════════════════════════════════════════════════════════╝
```

---

## Manual Method (Step-by-Step)

If you need more control or want to debug issues:

### Step 1: Identify Fix Files and Test Files

```bash
# Find the fix files (non-test code changes)
git diff main --name-only | grep -v "Test"

# Find the UI test files
git diff main --name-only | grep -E "TestCases\.(HostApp|Shared\.Tests)"
```

### Step 2: Identify the Test Name

UI tests follow the pattern `IssueXXXXX`:

```bash
# Find the issue number from test files
git diff main --name-only | grep -oE "Issue[0-9]+" | head -1
```

### Step 3: Run Tests WITH Fix (Baseline)

```bash
# Kill any existing Appium processes
lsof -i :4723 | grep LISTEN | awk '{print $2}' | xargs kill -9 2>/dev/null

# Run the tests
pwsh .github/scripts/BuildAndRunHostApp.ps1 -Platform ios -TestFilter "IssueXXXXX" 2>&1 | tee CustomAgentLogsTmp/UITests/test-with-fix.log
```

**Expected**: Tests should PASS

### Step 4: Revert the Fix

```bash
# Revert only the fix files (not the test files)
git checkout main -- path/to/fix/File1.cs path/to/fix/File2.cs
```

### Step 5: Run Tests WITHOUT Fix

```bash
# Kill any existing Appium processes
lsof -i :4723 | grep LISTEN | awk '{print $2}' | xargs kill -9 2>/dev/null

# Run the tests again
pwsh .github/scripts/BuildAndRunHostApp.ps1 -Platform ios -TestFilter "IssueXXXXX" 2>&1 | tee CustomAgentLogsTmp/UITests/test-without-fix.log
```

**Expected**: Tests should FAIL with a meaningful assertion error

### Step 6: Verify Failure Reason

```bash
# Check the test output
grep -A20 "Failed\|Assert\|Error" CustomAgentLogsTmp/UITests/test-output.log
```

**Good failure examples:**
- ✅ `Expected: "Choice 2", But was: "None"` - Correctly tests binding
- ✅ `Element 'ExpectedElement' should be visible` - Correctly tests rendering
- ✅ `Expected height: 100, Actual: 0` - Correctly tests layout

**Bad failure examples:**
- ❌ `Test crashed` - App is crashing, not testing the fix
- ❌ `Element not found` - Test is broken
- ❌ `Timeout` - Test or app is hanging

### Step 7: Check for App Crashes

If tests fail for unclear reasons, check device logs:

```bash
# Check device logs for actual error
grep -i "exception\|crash\|fatal" CustomAgentLogsTmp/UITests/ios-device.log | head -20
```

**Common crash causes:**
- Duplicate `[Issue]` attribute numbers
- Missing XAML event handlers
- Null reference in test page constructor

### Step 8: Restore the Fix

```bash
git checkout HEAD -- path/to/fix/File1.cs path/to/fix/File2.cs
```

## Output Format

```markdown
## UI Test Validation Results

**Platform**: iOS / Android
**Test Filter**: `IssueXXXXX`

### Regression Test Validation

| Scenario | Expected | Actual | Status |
|----------|----------|--------|--------|
| With fix | PASS | PASS/FAIL | ✅/❌ |
| Without fix | FAIL | PASS/FAIL | ✅/❌ |

### Failure Analysis (Without Fix)

**Assertion Error**:
```
[Quote the actual assertion failure message]
```
|
**Does failure match the issue?**
- ✅ Yes - Failure directly relates to the reported bug
- OR
- ❌ No - Failure is unrelated because [reason]
|
### Conclusion
|
- ✅ UI tests correctly validate the fix
- OR
- ⚠️ Tests need improvement because [reason]
```

## Common Issues

### Tests Pass Without Fix

**Possible causes:**
- Test checking wrong element/property
- Test has race condition (passes sometimes)
- Issue only occurs on specific platform

**Solutions:**
- Review test assertions and element locators
- Run test multiple times
- Test on the affected platform

### Tests Fail With Fix

**Possible causes:**
- Fix is incomplete
- Test has bugs
- Environment issue

**Solutions:**
- Check device logs for errors
- Verify fix addresses all scenarios
- Try different platform

### App Crashes

**Possible causes:**
- Duplicate issue numbers in `[Issue]` attributes
- XAML parse error
- Missing event handler

**Solutions:**
```bash
# Check for duplicate issue numbers
grep -r "IssueTracker.Github, XXXXX" src/Controls/tests/

# Check device logs
tail -100 CustomAgentLogsTmp/UITests/ios-device.log
```

### Element Not Found

**Possible causes:**
- Wrong AutomationId
- Element not rendered yet
- Element requires scrolling

**Solutions:**
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
