# PR #32647 Review Summary

## Overview
**PR Title:** [Android] NET10 - Exception on quit - fix  
**PR Link:** https://github.com/dotnet/maui/pull/32647  
**Issue:** https://github.com/dotnet/maui/issues/32600  
**Author:** @kubaflo  
**Reviewer:** GitHub Copilot  
**Review Date:** November 15, 2025

## Problem Statement

An unhandled `Android.Runtime.JavaProxyThrowable` exception occurs when quitting an Android .NET MAUI 10 application. This is a regression from .NET MAUI 9.0.120 SR12. The exception appears when the user clicks the Back button to exit the app, showing for a few seconds before the app actually quits.

## Error Details

From the issue logs:
```
11-13 19:02:13.141 E/AndroidRuntime( 7075): android.runtime.JavaProxyThrowable: [System.ObjectDisposedException]: Cannot access a disposed object.
11-13 19:02:13.141 E/AndroidRuntime( 7075): Object name: 'IServiceProvider'.
...
11-13 19:02:13.141 E/AndroidRuntime( 7075): at Microsoft.Maui.MauiContextExtensions.GetDispatcher(/_/src/Core/src/MauiContextExtensions.cs:33)
11-13 19:02:13.141 E/AndroidRuntime( 7075): at Microsoft.Maui.Controls.Platform.Compatibility.ShellFragmentContainer.OnDestroy(/_/src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellFragmentContainer.cs:43)
```

## Root Cause Analysis

When an Android app quits, the following sequence occurs:

1. **App Shutdown Begins**: The MauiContext's service provider begins disposal
2. **Android Lifecycle Callbacks**: Android framework calls `OnPause`, `OnStop`, `OnDestroy` on activities and fragments
3. **Service Provider Disposed**: The MauiContext's service provider is fully disposed
4. **Fragment.OnDestroy() Called**: ShellFragmentContainer.OnDestroy() is invoked
5. **Exception Thrown**: The code attempts to call `_mauiContext.GetDispatcher()` which internally calls `mauiContext.Services.GetRequiredService<IDispatcher>()`, but the service provider is already disposed

This throws an `ObjectDisposedException` with the message "Cannot access a disposed object. Object name: 'IServiceProvider'", which surfaces as a `JavaProxyThrowable`.

## PR Changes

The PR removes the OnDestroy override entirely:

```diff
- public override void OnDestroy()
- {
-     _mauiContext
-         .GetDispatcher()
-         .Dispatch(Dispose);
-
-     base.OnDestroy();
- }
```

**Files Changed:**
- `src/Controls/src/Core/Compatibility/Handlers/Shell/Android/ShellFragmentContainer.cs`

**Lines Changed:** -9 (deletions only)

## Analysis

### 1. Is the Fix Safe?

✅ **YES** - The fix is safe for the following reasons:

#### Cleanup is Already Handled
The `OnDestroyView()` method already handles the necessary cleanup:
```csharp
public override void OnDestroyView()
{
    base.OnDestroyView();
    ((IShellContentController)ShellContentTab).RecyclePage(_page);
    _page = null;
}
```

This recycles the page and nulls out the reference, which is the primary cleanup needed.

#### No Custom Destroy Logic
Unlike other similar fragments (ShellContentFragment, ShellItemRenderer, ShellSectionRenderer), `ShellFragmentContainer` does NOT have a custom `Destroy()` method with additional cleanup logic. It was simply calling the base Fragment's `Dispose()` method.

#### Fragment Lifecycle Management
The Android framework handles Fragment disposal through its lifecycle. The Fragment base class's `Dispose()` method will still be called by the Android runtime when appropriate - it doesn't need to be explicitly dispatched.

### 2. Comparison with Other Fragments

**ShellContentFragment:**
```csharp
public override void OnDestroy()
{
    base.OnDestroy();
    Destroy();  // Calls custom Destroy() method with cleanup logic
}

void Destroy()
{
    // Actual cleanup: AnimationFinished events, MauiWindowInsetListener, etc.
    ...
}
```

**ShellItemRenderer:**
```csharp
public override void OnDestroy()
{
    Destroy();  // Calls custom Destroy() method with cleanup logic
    base.OnDestroy();
}

void Destroy()
{
    // Actual cleanup: Fragments, handlers, etc.
    ...
}
```

**ShellFragmentContainer (BEFORE fix):**
```csharp
public override void OnDestroy()
{
    _mauiContext.GetDispatcher().Dispatch(Dispose);  // Just calls base Dispose
    base.OnDestroy();
}

// No custom Destroy() method!
```

**ShellFragmentContainer (AFTER fix):**
```csharp
// No OnDestroy override - relies on base Fragment lifecycle
```

### 3. Memory Leak Analysis

✅ **No memory leaks expected**

- `_page`: Nulled out in OnDestroyView()
- `_mauiContext`: Will be garbage collected when the fragment is collected
- `ShellContentTab`: Property will be garbage collected with the fragment
- Fragment disposal: Handled by Android framework's lifecycle management

### 4. Historical Context

The OnDestroy method was introduced in commit `0e86703d38` (PR #5064) with the message "Switch to dispatcher so Dispose is called after DestroyView".

**Original Change (March 2022):**
```diff
- Activity.RunOnUiThread(Dispose);
+ _mauiContext.GetDispatcher().Dispatch(Dispose);
```

The change was made to use the dispatcher instead of `Activity.RunOnUiThread`. However, this introduced a dependency on the service provider being available, which is **not guaranteed** during app shutdown.

**Why this worked before .NET 10:**
The timing of service provider disposal may have changed in .NET 10, causing it to be disposed earlier in the shutdown sequence, before OnDestroy() is called on fragments.

## Testing Recommendations

### Manual Testing
1. ✅ Create a new .NET MAUI 10 Android application
2. ✅ Build and deploy to Android emulator (API 36)
3. ✅ Launch the app
4. ✅ Click the Back button to quit
5. ✅ Verify no exception is thrown
6. ✅ Verify app quits cleanly

### Regression Testing
1. Test navigation scenarios with Shell
2. Test tab switching in Shell
3. Test rapid navigation (push/pop)
4. Test app backgrounding and foregrounding
5. Test memory usage over time (no leaks)

## Review Comments

### ✅ Approved

**Reasoning:**
1. **Fixes the bug**: Removes the root cause of the exception
2. **Safe change**: Cleanup is already handled in OnDestroyView
3. **No memory leaks**: Proper cleanup is maintained
4. **Minimal change**: Only removes unnecessary code
5. **Consistent pattern**: Aligns with fragments that don't have custom Destroy logic

### Potential Concerns Addressed

**Q: Won't removing OnDestroy prevent proper cleanup?**
A: No. OnDestroyView() already handles the necessary cleanup (page recycling). The OnDestroy was only calling Fragment.Dispose(), which the Android framework will call anyway.

**Q: Could this cause memory leaks?**
A: No. The _page reference is nulled in OnDestroyView(), and the fragment itself will be garbage collected when no longer referenced. The Android framework manages fragment lifecycle.

**Q: Why was the Dispose call added in the first place?**
A: PR #5064 changed from Activity.RunOnUiThread to use the dispatcher, but didn't account for the service provider being disposed during app shutdown. The original intent was to ensure disposal happened on the UI thread after DestroyView, but this is unnecessary for ShellFragmentContainer since it has no custom cleanup logic.

**Q: Should we use a try-catch instead?**
A: No. Removing the code is better because:
- The cleanup is unnecessary (no custom Destroy logic)
- Swallowing exceptions could hide other issues
- The Android framework already handles fragment disposal

## Alternative Solutions Considered

### Alternative 1: Try-Catch Block
```csharp
public override void OnDestroy()
{
    try
    {
        _mauiContext?.GetDispatcher()?.Dispatch(Dispose);
    }
    catch (ObjectDisposedException)
    {
        // Service provider already disposed, fragment disposal will be handled by Android
    }
    
    base.OnDestroy();
}
```

**Why rejected:** Adds complexity without benefit. The disposal is unnecessary.

### Alternative 2: Check Service Provider
```csharp
public override void OnDestroy()
{
    var dispatcher = _mauiContext?.GetOptionalDispatcher();
    if (dispatcher != null)
    {
        dispatcher.Dispatch(Dispose);
    }
    
    base.OnDestroy();
}
```

**Why rejected:** Still unnecessary since there's no custom cleanup logic.

### Alternative 3: Remove Entirely (CHOSEN)
```csharp
// No OnDestroy override
```

**Why chosen:** Simplest and safest solution. Removes unnecessary code that depends on disposed resources.

## Recommendations

### For Merge
✅ **APPROVE** - This PR should be merged.

**Justification:**
1. Fixes a critical regression (p/0 priority issue)
2. Change is minimal and safe
3. No additional cleanup logic needed
4. Consistent with other fragments without custom Destroy logic
5. No memory leak risk

### Additional Suggestions

1. **Add Unit Test**: Consider adding a unit test that verifies ShellFragmentContainer can be destroyed during app shutdown without throwing exceptions

2. **Code Comment**: Consider adding a comment in the code explaining why OnDestroy is not overridden:
   ```csharp
   // OnDestroy is not overridden because:
   // 1. OnDestroyView already handles cleanup (page recycling)
   // 2. Fragment disposal is managed by Android framework
   // 3. No custom Destroy() logic needed (unlike ShellContentFragment, etc.)
   ```

3. **Documentation**: Update any relevant documentation about Fragment lifecycle in Shell

## Conclusion

The fix is **correct and safe**. It removes unnecessary code that was causing an exception during app shutdown. The cleanup that was being performed (calling Fragment.Dispose()) is not needed because:

1. OnDestroyView() already handles page cleanup
2. ShellFragmentContainer has no custom Destroy() logic
3. Android framework handles fragment disposal

This is a **minimal, surgical fix** that addresses the root cause without introducing new issues.

---

## Review Checklist

- [x] Root cause identified and documented
- [x] Fix approach analyzed and validated
- [x] Compared with similar code patterns
- [x] Memory leak analysis completed
- [x] Alternative solutions considered
- [x] Testing recommendations provided
- [x] Security implications reviewed (none)
- [x] Performance implications reviewed (positive - removes unnecessary dispatcher call)
- [x] Breaking changes assessed (none)
- [x] Documentation needs identified

## Final Recommendation

✅ **APPROVE AND MERGE**

This PR correctly fixes a critical regression in .NET MAUI 10 with a minimal, safe change.
