# iOS Testing Guide for PR #32815

## Quick Start (For macOS Tester)

This PR fixes NavigationPage.TitleView not resizing on iOS 26+ when device rotates.

### Prerequisites
- macOS with Xcode installed
- iPhone Xs simulator with iOS 26+
- PowerShell Core (`brew install powershell`)

### Test Sequence

#### 1. Test WITH PR Fix (should pass)

```bash
cd /home/runner/work/maui/maui
pwsh .github/scripts/BuildAndRunSandbox.ps1 -Platform ios
```

**Expected Output**:
```
✅ TEST PASSED: TitleView resizes correctly!
   Initial (Portrait): ~375px
   Landscape:          ~667px  ← Width changes!
   Final (Portrait):   ~375px
```

#### 2. Test WITHOUT PR Fix (should fail - baseline)

```bash
# Revert the fix
git checkout origin/main -- src/Controls/src/Core/Compatibility/Handlers/NavigationPage/iOS/NavigationRenderer.cs

# Verify revert worked (should show no diff)
git diff origin/main -- src/Controls/src/Core/Compatibility/Handlers/NavigationPage/iOS/NavigationRenderer.cs

# Run test
pwsh .github/scripts/BuildAndRunSandbox.ps1 -Platform ios
```

**Expected Output**:
```
❌ BUG REPRODUCED: TitleView does NOT resize!
   Initial (Portrait): ~375px
   Landscape:          ~375px  ← Width doesn't change (BUG!)
   Final (Portrait):   ~375px
```

#### 3. Restore PR Fix

```bash
git checkout HEAD -- src/Controls/src/Core/Compatibility/Handlers/NavigationPage/iOS/NavigationRenderer.cs
```

### What to Report

Please provide:
1. Console output from both test runs (WITH and WITHOUT)
2. Screenshots from `SandboxAppium/` directory showing:
   - `titleview_initial.png` (Portrait)
   - `titleview_landscape.png` (Landscape)
   - `titleview_final.png` (Portrait again)
3. Confirmation whether results match expectations above

### Additional Console Logs to Check

Look for these markers in the output:
- `TITLEVIEW MEASUREMENTS:` - Shows detailed measurements from instrumentation
- `[iOS] Platform Frame:` - Shows iOS UIView frame information
- `[iOS] AutoresizingMask:` - Should show FlexibleWidth flag

### Troubleshooting

**App won't build**:
```bash
# Clean and rebuild
dotnet clean
dotnet build src/Controls/samples/Controls.Sample.Sandbox/Maui.Controls.Sample.Sandbox.csproj -f net10.0-ios
```

**Appium connection errors**:
- The script automatically starts/stops Appium
- Check `SandboxAppium/appium.log` for details

**No iOS 26+ simulator**:
- Open Xcode
- Go to Xcode → Settings → Platforms
- Download iOS 26+ runtime
- Create iPhone Xs simulator with iOS 26+

### Understanding the Results

**Why test both WITH and WITHOUT?**
- WITH: Proves the fix works
- WITHOUT: Proves the bug actually exists (not a false positive)

**Why these specific width values?**
- iPhone Xs Portrait: ~375pt wide
- iPhone Xs Landscape: ~667pt wide
- TitleView should match screen width in each orientation

**What if results don't match expectations?**
- Still report them! Unexpected results are valuable feedback
- Include full console output
- Mention iOS version and device model used
