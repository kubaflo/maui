# PR #32815 Review Feedback - Issue #32722

## Overview
This document contains the review feedback for PR #32815 which addresses issue #32722: NavigationPage.TitleView does not expand with host window in iPadOS 26+.

**PR**: https://github.com/dotnet/maui/pull/32815  
**Issue**: https://github.com/dotnet/maui/issues/32722  
**Review Date**: November 23, 2025

---

## Executive Summary
✅ **APPROVED - Ready to merge after applying recommended improvements**

This PR successfully fixes issue #32722 where NavigationPage.TitleView does not expand with the host window in iPadOS 26+. The implementation is technically sound, well-tested, and follows MAUI coding patterns.

---

## What This PR Does
Adds logic to update the TitleView frame during orientation changes for iOS 26+ and Mac Catalyst 26+, ensuring the TitleView expands to fill the navigation bar after rotation.

### The Fix
1. Overrides `TraitCollectionDidChange` in ParentingViewController class
2. Detects orientation changes via size class transitions  
3. Explicitly updates TitleView frame to match navigation bar dimensions on iOS 26+
4. Includes comprehensive test coverage

---

## Files Changed
1. `src/Controls/src/Core/Compatibility/Handlers/NavigationPage/iOS/NavigationRenderer.cs` - Core fix
2. `src/Controls/tests/TestCases.HostApp/Issues/Issue32722.xaml` - Test page XAML
3. `src/Controls/tests/TestCases.HostApp/Issues/Issue32722.xaml.cs` - Test page code-behind
4. `src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue32722.cs` - UI test

---

## Review Findings

### ✅ Strengths (What's Great)
- Correctly identifies and fixes the root cause
- Uses appropriate iOS API (TraitCollectionDidChange)
- Properly scoped to iOS 26+ and MacCatalyst 26+ only
- Excellent null safety checks
- Clear documentation
- Consistent with existing iOS 26 workaround (PR #32341)
- No breaking changes
- No security concerns (CodeQL verified)
- No performance issues
- Build verification passed

### 📝 Recommended Improvements (Minor Polish)

I've identified three minor improvements that will enhance code quality and test reliability.

#### 1. Documentation Fix (1 line change)
**File**: `NavigationRenderer.cs` line 1613  
**Issue**: Missing period after closing parenthesis

**Current**:
```csharp
/// iOS 26+ requires autoresizing masks (UIViewAutoresizing.FlexibleWidth) During orientation changes
```

**Recommended**:
```csharp
/// iOS 26+ requires autoresizing masks (UIViewAutoresizing.FlexibleWidth). During orientation changes
```

#### 2. Test Reliability Improvement (2 instances)
**File**: `Issue32722.cs` lines 27-28 and 46  
**Issue**: Using Thread.Sleep makes tests brittle and potentially slower

**Current**:
```csharp
App.SetOrientationLandscape();
System.Threading.Thread.Sleep(2000);
```

**Recommended**:
```csharp
App.SetOrientationLandscape();
// Wait for rotation to complete and ensure the element is updated
App.WaitForElement("TitleViewGrid");
```

**Why**: Follows pattern in other rotation tests (CarouselViewUITests.cs), more reliable, potentially faster

#### 3. Test Organization Improvement (1 line change)
**File**: `Issue32722.cs` line 14  
**Issue**: Test category doesn't match the specific functionality being tested

**Current**:
```csharp
[Category(UITestCategories.Navigation)]
```

**Recommended**:
```csharp
[Category(UITestCategories.TitleView)]
```

**Why**: Better test categorization - test specifically validates TitleView behavior

---

## How to Apply Improvements

### Option 1: Apply the Patch File
A patch file `recommended_improvements.patch` is available with all three improvements:

```bash
git apply recommended_improvements.patch
```

### Option 2: Manual Changes
Make the three changes listed above manually in the files.

---

## Quality Metrics
- **Code Quality**: 9.5/10
- **Fix Correctness**: 10/10
- **Test Coverage**: 9/10
- **Overall Score**: 9.5/10

---

## Technical Details

### Implementation Approach
The fix adds a `TraitCollectionDidChange` override that:
1. Detects when orientation changes (size class transitions)
2. For iOS 26+ only, calls `UpdateTitleViewFrameForOrientation()`
3. Explicitly sets TitleView frame to match navigation bar dimensions
4. Calls `LayoutIfNeeded()` to apply changes immediately

### Why This Works
iOS 26+ uses autoresizing masks that automatically adjust width, but the TitleView needs an explicit frame update to expand properly. This fix ensures the frame matches the navigation bar dimensions after rotation.

### Code Flow
1. User rotates device or resizes window
2. TraitCollectionDidChange fires with previous trait collection
3. Check if size classes changed (orientation change)
4. If iOS 26+ or MacCatalyst 26+, call UpdateTitleViewFrameForOrientation()
5. UpdateTitleViewFrameForOrientation() sets TitleView frame to navigation bar dimensions
6. Call LayoutIfNeeded() to apply changes

### Relationship to PR #32341
This PR complements PR #32341 which fixed the initial iOS 26 TitleView issue:
- **PR #32341**: Set initial Container frame from nav bar at creation
- **PR #32815**: Update Container frame on rotation/window resize
- Both use the same approach: Explicitly set frame from navigation bar dimensions

---

## Build Verification ✅
All checks passed:
- ✅ dotnet tool restore
- ✅ dotnet build Microsoft.Maui.BuildTasks.slnf (0 errors, 0 warnings)
- ✅ dotnet format (code formatted correctly)
- ✅ CodeQL security scan (no issues detected)

---

## Test Coverage ✅
- Test page with visible TitleView (Issue32722.xaml)
- UI test verifying rotation behavior (Issue32722.cs)
- Tests verify:
  - TitleView width changes on rotation
  - TitleView returns to original width when rotated back
  - TitleView has reasonable dimensions after rotation

### Test Scenarios (Manual Testing)
If testing with iOS environment:

1. **Basic rotation test (Portrait → Landscape → Portrait)**
   - Launch Issue32722 test page
   - Verify TitleView fills navigation bar in portrait
   - Rotate to landscape → TitleView should expand to fill wider nav bar
   - Rotate back to portrait → TitleView should contract back to original width

2. **iPad split-view test**
   - Open app in narrow split-view window
   - Drag to expand window width
   - TitleView should expand with window

3. **Rapid rotation test**
   - Rapidly rotate device multiple times
   - No crashes, no visual artifacts
   - TitleView tracks navigation bar width correctly

---

## Final Recommendation
**APPROVE AND MERGE** ✅

The PR is technically sound and ready for production. The three recommended improvements are minor refinements that enhance polish but don't affect functionality. The PR can be merged as-is, or with the improvements applied for optimal code quality.

---

## Patch File

The following patch can be applied to incorporate all recommended improvements:

```diff
diff --git a/src/Controls/src/Core/Compatibility/Handlers/NavigationPage/iOS/NavigationRenderer.cs b/src/Controls/src/Core/Compatibility/Handlers/NavigationPage/iOS/NavigationRenderer.cs
index e57c3dab60..9a413226c7 100644
--- a/src/Controls/src/Core/Compatibility/Handlers/NavigationPage/iOS/NavigationRenderer.cs
+++ b/src/Controls/src/Core/Compatibility/Handlers/NavigationPage/iOS/NavigationRenderer.cs
@@ -1610,7 +1610,7 @@ namespace Microsoft.Maui.Controls.Handlers.Compatibility
 				}
 			}
 
-			/// iOS 26+ requires autoresizing masks (UIViewAutoresizing.FlexibleWidth) During orientation changes, the autoresizing mask
+			/// iOS 26+ requires autoresizing masks (UIViewAutoresizing.FlexibleWidth). During orientation changes, the autoresizing mask
 			/// automatically adjusts the width, but we need to explicitly update the frame to ensure the
 			/// title view uses the full available width from the navigation bar. Without this update,
 			/// the title view may not properly expand to fill the navigation bar after rotation.
diff --git a/src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue32722.cs b/src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue32722.cs
index aad1443984..f010dc403b 100644
--- a/src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue32722.cs
+++ b/src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue32722.cs
@@ -11,7 +11,7 @@ namespace Microsoft.Maui.TestCases.Tests.Issues
 		public Issue32722(TestDevice device) : base(device) { }
 
 		[Test]
-		[Category(UITestCategories.Navigation)]
+		[Category(UITestCategories.TitleView)]
 		public void TitleViewExpandsOnRotation()
 		{
 			// Wait for page to load
@@ -21,11 +21,11 @@ namespace Microsoft.Maui.TestCases.Tests.Issues
 			// Get initial orientation and TitleView bounds
 			var titleViewInitial = App.WaitForElement("TitleViewGrid").GetRect();
 			var initialWidth = titleViewInitial.Width;
-			
+
 			App.SetOrientationLandscape();
 
-			// Wait for rotation to complete
-			System.Threading.Thread.Sleep(2000);
+			// Wait for rotation to complete and ensure the element is updated
+			App.WaitForElement("TitleViewGrid");
 
 			// Get TitleView bounds after rotation
 			var titleViewAfterRotation = App.WaitForElement("TitleViewGrid").GetRect();
@@ -34,16 +34,18 @@ namespace Microsoft.Maui.TestCases.Tests.Issues
 			// On iOS 26+, the TitleView should expand/contract with the rotation
 			// The bug was that it would stay at the original width
 			// After fix, the width should change to match the new navigation bar width
-			Assert.That(newWidth, Is.Not.EqualTo(initialWidth).Within(100), 
+			Assert.That(newWidth, Is.Not.EqualTo(initialWidth).Within(100),
 				"TitleView width should change after rotation");
 
 			// Verify TitleView is still visible and has reasonable dimensions
-			Assert.That(newWidth, Is.GreaterThan(100), 
+			Assert.That(newWidth, Is.GreaterThan(100),
 				"TitleView should have a reasonable width after rotation");
 
 			// Rotate back to original orientation
 			App.SetOrientationPortrait();
-			System.Threading.Thread.Sleep(2000);
+
+			// Wait for rotation to complete and ensure the element is updated
+			App.WaitForElement("TitleViewGrid");
 
 			// Verify TitleView returns to approximately original width
 			var titleViewFinal = App.WaitForElement("TitleViewGrid").GetRect();
```

---

*Review completed by: GitHub Copilot Coding Agent*  
*Review date: November 23, 2025*  
*PR: dotnet/maui#32815*  
*Issue: dotnet/maui#32722*
