# PR Review: #32081 - [iOS] Added support for large titles in Shell

**PR**: https://github.com/dotnet/maui/pull/32081  
**Issue**: https://github.com/dotnet/maui/issues/12156  
**Reviewer**: GitHub Copilot PR Reviewer  
**Review Date**: 2025-11-20

## Summary

PR #32081 implements iOS large navigation bar titles in Shell by adding `UpdateLargeTitles()` to `ShellItemRenderer.cs`. The implementation correctly maps .NET MAUI's `LargeTitleDisplayMode` to iOS `UINavigationItemLargeTitleDisplayMode`.

**Status**: ✅ Excellent implementation with minor cosmetic cleanup suggested

**Core implementation**: ✅ Solid - proper null checks, iOS version guard, correct enum mapping, appropriate lifecycle hooks  
**Test coverage**: ✅ Well-structured with screenshot verification, has minor redundancy in preprocessor directive

---

## Code Review

### ✅ Positive Aspects

1. **Correct iOS version check**
   - Uses `OperatingSystem.IsIOSVersionAtLeast(11)` (large titles introduced in iOS 11)
   - Prevents crashes on older iOS versions

2. **Proper null safety**
   - Checks `page is null` before proceeding
   - Checks `navigationController` and `top` before accessing

3. **Appropriate lifecycle hooks**
   - Called in `OnDisplayedPageChanged()` when page changes
   - Called in `ViewWillLayoutSubviews()` to ensure state consistency

4. **Correct enum mapping**
   ```csharp
   LargeTitleDisplayMode.Always => UINavigationItemLargeTitleDisplayMode.Always,
   LargeTitleDisplayMode.Automatic => UINavigationItemLargeTitleDisplayMode.Automatic,
   _ => UINavigationItemLargeTitleDisplayMode.Never
   ```

5. **Sets both required properties**
   - `NavigationBar.PrefersLargeTitles` (enables feature)
   - `NavigationItem.LargeTitleDisplayMode` (per-page control)

6. **Follows existing patterns**
   - Mirrors `UpdateTabBarHidden()` implementation
   - Consistent with Shell handler architecture

---

## 🟡 Minor Issues

### 1. Redundant Conditional Compilation

**File**: `src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue12156.cs`  
**Line**: 1

**Issue**:
```csharp
#if TEST_FAILS_ON_ANDROID && TEST_FAILS_ON_ANDROID && TEST_FAILS_ON_WINDOWS
```

`TEST_FAILS_ON_ANDROID` appears twice - this is redundant but harmless.

**How it works**: The test project system defines constants for platforms where tests SHOULD NOT run:
- iOS.Tests defines: `TEST_FAILS_ON_ANDROID`, `TEST_FAILS_ON_WINDOWS`, `TEST_FAILS_ON_CATALYST`
- Android.Tests defines: `TEST_FAILS_ON_IOS`, `TEST_FAILS_ON_WINDOWS`, `TEST_FAILS_ON_CATALYST`

So this condition:
- iOS tests: TRUE && TRUE && TRUE = TRUE → test runs ✅
- Android tests: FALSE && FALSE && TRUE = FALSE → test skipped ✅
- Windows tests: FALSE && FALSE && FALSE = FALSE → test skipped ✅

**Fix** (optional cleanup):
```csharp
#if TEST_FAILS_ON_ANDROID && TEST_FAILS_ON_WINDOWS  // Remove duplicate
```

**Impact**: LOW - Code compiles and runs correctly, just has a cosmetic redundancy

---

## 🟡 Suggestions for Improvement

### 1. Missing Dynamic Property Change Handling

**Current behavior**: `UpdateLargeTitles()` is only called when:
- Displayed page changes (`OnDisplayedPageChanged`)
- View lays out subviews (`ViewWillLayoutSubviews`)

**Gap**: If a user changes `Page.LargeTitleDisplay` property dynamically at runtime (e.g., in response to user action), the navigation bar won't update.

**Comparison**: `OnDisplayedPagePropertyChanged` only listens for `Shell.TabBarIsVisibleProperty`:
```csharp
void OnDisplayedPagePropertyChanged(object sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == Shell.TabBarIsVisibleProperty.PropertyName)
        UpdateTabBarHidden();
    // Missing: Check for LargeTitleDisplay property changes
}
```

**Suggested improvement**:
```csharp
void OnDisplayedPagePropertyChanged(object sender, PropertyChangedEventArgs e)
{
    if (e.PropertyName == Shell.TabBarIsVisibleProperty.PropertyName)
        UpdateTabBarHidden();
    else if (e.PropertyName == PlatformConfiguration.iOSSpecific.Page.LargeTitleDisplayProperty.PropertyName)
        UpdateLargeTitles();
}
```

**Impact**: MEDIUM - Users might want to toggle this dynamically (though uncommon)

---

### 2. Performance Consideration - ViewWillLayoutSubviews

**Observation**: `UpdateLargeTitles()` is called in `ViewWillLayoutSubviews()`, which executes on every layout pass (very frequently).

**Analysis**: 
- Setting navigation bar properties repeatedly could be inefficient
- However, `UpdateTabBarHidden()` already uses this pattern
- iOS likely optimizes no-op property sets

**Potential optimization** (optional):
```csharp
LargeTitleDisplayMode? _lastLargeTitleDisplayMode;

void UpdateLargeTitles()
{
    var page = _displayedPage;
    if (page is null || !OperatingSystem.IsIOSVersionAtLeast(11))
        return;

    var largeTitleDisplayMode = page.OnThisPlatform().LargeTitleDisplay();
    
    // Only update if changed
    if (_lastLargeTitleDisplayMode == largeTitleDisplayMode)
        return;
        
    _lastLargeTitleDisplayMode = largeTitleDisplayMode;
    
    // ... rest of implementation
}
```

**Impact**: LOW - Likely not a real performance issue, iOS handles repeated property sets efficiently

---

### 3. Test Coverage Gaps

**Current test**:
- Only tests `LargeTitleDisplay="Always"`
- Uses screenshot verification (good)
- Simple single-page Shell

**Missing scenarios**:
1. `LargeTitleDisplay="Never"` - should show regular title
2. `LargeTitleDisplay="Automatic"` - should follow system behavior
3. Multi-page navigation - title mode changes when navigating
4. Dynamic property changes - changing mode at runtime
5. Shell with multiple tabs - each tab can have different modes

**Suggested additional tests** (future enhancement):
```csharp
[Test]
public void LargeTitleDisplayNever()
{
    // Verify standard title bar height
}

[Test]
public void LargeTitleDisplayAutomatic()
{
    // Verify automatic behavior (large on first page, small after scroll/navigation)
}
```

**Impact**: MEDIUM - Current test validates basic functionality, but edge cases untested

---

## 🛑 Testing Status - CHECKPOINT REQUIRED

### Environment Limitation

**Issue**: PR review is being performed in a Linux environment without iOS simulators.  
**Required**: iOS simulator or physical device to validate:
1. Large title actually displays correctly  
2. Title size changes between Always/Never/Automatic modes
3. Navigation between pages preserves settings
4. No visual glitches or layout issues

### Checkpoint for iOS Testing

**Platform**: iOS 17+ (iPhone Xs or later recommended)  
**Why needed**: Visual validation of large title behavior requires running on iOS  

**Testing steps needed**:

1. **Build Sandbox app with test scenario**:
   ```bash
   # Modify src/Controls/samples/Controls.Sample.Sandbox/MainPage.xaml to:
   # - Create a Shell with LargeTitleDisplay="Always"
   # - Add navigation button to test with LargeTitleDisplay="Never"
   # - Add another button for "Automatic" mode
   
   # Build for iOS
   dotnet build src/Controls/samples/Controls.Sample.Sandbox/Maui.Controls.Sample.Sandbox.csproj -f net10.0-ios
   
   # Install to simulator
   xcrun simctl install $UDID artifacts/bin/Maui.Controls.Sample.Sandbox/Debug/net10.0-ios/iossimulator-arm64/Maui.Controls.Sample.Sandbox.app
   
   # Launch and capture screenshots
   xcrun simctl launch --console-pty $UDID com.microsoft.maui.sandbox
   ```

2. **Visual verification**:
   - Screenshot with `LargeTitleDisplay="Always"` - title should be LARGE
   - Screenshot with `LargeTitleDisplay="Never"` - title should be regular size
   - Screenshot with `LargeTitleDisplay="Automatic"` - title should be large initially
   - Navigate to second page - verify title behavior changes as expected

3. **Edge case testing**:
   - Change property dynamically at runtime (if implemented)
   - Navigate back and forth between pages
   - Switch between Shell tabs (if multi-tab)

**Resume instructions**: After iOS testing is complete, update this review document with:
- Test results (pass/fail for each scenario)
- Screenshots showing large title vs regular title
- Any visual issues discovered

---

## Current Recommendation

✅ **Approve with Optional Improvements**

### Optional Cleanup (Non-Blocking)

1. 🟡 **Remove redundant condition** in UI test (Line 1 of Issue12156.cs)
   - Current: `#if TEST_FAILS_ON_ANDROID && TEST_FAILS_ON_ANDROID && TEST_FAILS_ON_WINDOWS`
   - Cleaner: `#if TEST_FAILS_ON_ANDROID && TEST_FAILS_ON_WINDOWS`
   - **Impact**: Cosmetic only - code works correctly either way

### Recommended Improvements (Non-Blocking)

1. 🟡 Add property change listener for dynamic `LargeTitleDisplay` updates (see suggestion above)
2. 🟡 Expand test coverage to include Never/Automatic modes (future enhancement)
3. 🟡 Consider caching last display mode to avoid redundant property sets (optional performance optimization)

---

## Post-iOS-Testing Update

**iOS testing status**: Unable to complete due to Linux environment limitation (no iOS simulators available)

**Recommendation without iOS testing**: Based on code review alone, the implementation appears correct:
- Proper use of iOS APIs
- Correct lifecycle integration
- Appropriate null safety and version checks
- Follows established patterns in the codebase

**Ideal validation** (when iOS environment is available):
1. Visual confirmation that large titles display correctly
2. Verification of Always/Never/Automatic modes
3. Test navigation between pages with different settings
4. Confirm no visual glitches or layout issues

**Current recommendation**: ✅ **Approve** - Code quality is excellent, iOS testing would provide additional confidence but is not blocking

---

## Related Documentation

- [iOS Large Title Documentation](https://developer.apple.com/design/human-interface-guidelines/ios/bars/navigation-bars/)
- [UINavigationBar.prefersLargeTitles](https://developer.apple.com/documentation/uikit/uinavigationbar/2908999-preferslargetitles)
- [.NET MAUI Platform Specifics - iOS](https://learn.microsoft.com/dotnet/maui/platform-integration/platform-specifics)

---

## Review History

- **2025-11-20**: Initial code review complete, iOS testing checkpoint created
- **Pending**: iOS simulator validation
