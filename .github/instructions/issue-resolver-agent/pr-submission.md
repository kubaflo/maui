# PR Submission Guidelines

## Creating the Pull Request

### PR Title Format

**Required format**: `[Issue-Resolver] Fix #XXXXX - <Brief Description>`

**Examples:**
- `[Issue-Resolver] Fix #12345 - CollectionView RTL padding incorrect on iOS`
- `[Issue-Resolver] Fix #67890 - Label text truncation with SafeArea enabled`
- `[Issue-Resolver] Fix #11111 - Entry cursor position wrong in RTL mode`

**The `[Issue-Resolver]` prefix:**
- Identifies PRs created by the issue-resolver agent
- Helps maintainers track agent-generated contributions
- Distinguishes from community PRs

### PR Description Template

**Complete template to use:**

```markdown
Fixes #XXXXX

## Description

[Brief description of what the issue was and what this PR fixes]

Example: 
"CollectionView was not applying correct padding in RTL mode on iOS because the compositional layout wasn't configured to respect the semantic content attribute."

## Root Cause

[Technical explanation of WHY the bug existed]

Example:
"The `UpdateFlowDirection` method in `CollectionViewHandler.iOS.cs` only set the `SemanticContentAttribute` on the UICollectionView itself, but UICollectionViewCompositionalLayout requires explicit configuration on the layout object to properly handle RTL scenarios. The layout continued using LTR direction regardless of the view's semantic attribute."

## Solution

[Explanation of HOW your fix resolves the root cause]

Example:
"Added a new extension method `UpdateLayoutDirection()` that configures the UICollectionViewCompositionalLayout's scroll direction based on FlowDirection. This method is called whenever FlowDirection changes, ensuring the layout properly mirrors content in RTL mode."

**Changes made:**
- Modified `CollectionViewHandler.iOS.cs` to update layout configuration
- Added `UpdateLayoutDirection()` extension method
- Updated `MapFlowDirection()` to configure both view and layout

## Testing

### Reproduction Verified

**Platform**: iOS 18.0 (iPhone 15 Pro Simulator)
**Build**: Debug, net10.0-ios

**Before fix:**
```
=== STATE CAPTURE: AfterTrigger ===
Control Bounds: {X=0 Y=0 Width=393 Height=600}
Left Padding: 0px ❌ (Expected: 16px)
Right Padding: 16px ❌ (Expected: 0px)
```

**After fix:**
```
=== STATE CAPTURE: AfterTrigger ===
Control Bounds: {X=0 Y=0 Width=393 Height=600}
Left Padding: 16px ✅
Right Padding: 0px ✅
```

### Edge Cases Tested

| Scenario | Result |
|----------|--------|
| Empty ItemsSource | ✅ No crash, handles correctly |
| Null ItemsSource | ✅ Handles gracefully |
| Rapid FlowDirection toggling (10x) | ✅ No flicker or incorrect state |
| FlowDirection.MatchParent | ✅ Inherits correctly from parent |
| With headers and footers | ✅ Padding applies to all sections |
| While scrolling | ✅ Updates correctly without interruption |
| Nested in ScrollView | ✅ Both controls handle RTL correctly |

### Platforms Tested

- ✅ iOS 18.0 (Simulator)
- ✅ Android 14.0 (Emulator) - Verify no regression
- ⏭️ Windows - Not affected by this iOS-specific issue
- ⏭️ MacCatalyst - Same code path as iOS, assumed working

### Related Scenarios Verified

Verified these related scenarios still work correctly:
- ✅ FlowDirection on ListView
- ✅ FlowDirection on Grid layouts
- ✅ Nested RTL controls
- ✅ CollectionView with different ItemsLayout types

## Test Coverage

**UI Tests Added:**
- ✅ Test page: `src/Controls/tests/TestCases.HostApp/Issues/Issue12345.xaml`
- ✅ NUnit test: `src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue12345.cs`

**Test validates:**
- CollectionView displays with correct RTL padding
- Padding values are swapped correctly (left/right)
- Screenshot verification confirms visual correctness

## Breaking Changes

**None** - This fix is purely a bug fix with no API changes or behavior changes to correct functionality.

[OR if there are breaking changes:]

**⚠️ Breaking Changes:**
- [List any breaking changes]
- [Explain why they are necessary]
- [Provide migration guidance]

## Additional Notes

[Any other context for reviewers]

Examples:
- "This fix only affects iOS. Android already handles this correctly."
- "Considered alternative approach X, but rejected because Y."
- "This may need backporting to release/X.X branch."
```

### Writing UI Tests

**Required for every fix**: Create automated tests to prevent regressions.

#### Step 1: Create Test Page

**File**: `src/Controls/tests/TestCases.HostApp/Issues/Issue12345.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             x:Class="Maui.Controls.Sample.Issues.Issue12345"
             Title="Issue 12345 - CollectionView RTL Padding">

    <VerticalStackLayout>
        <!-- Reproduce the issue scenario -->
        <CollectionView x:Name="TestCollection"
                        AutomationId="TestCollection"
                        FlowDirection="RightToLeft"
                        ItemsSource="{Binding Items}">
            <CollectionView.ItemTemplate>
                <DataTemplate>
                    <Label Text="{Binding .}" 
                           Padding="16,8"
                           AutomationId="ItemLabel"/>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
        
        <Label x:Name="StatusLabel"
               AutomationId="StatusLabel"
               Text="Ready"/>
    </VerticalStackLayout>
</ContentPage>
```

**File**: `src/Controls/tests/TestCases.HostApp/Issues/Issue12345.xaml.cs`

```csharp
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace Maui.Controls.Sample.Issues
{
    [Issue(IssueTracker.Github, 12345, "CollectionView RTL padding incorrect", 
           PlatformAffected.iOS)]
    public partial class Issue12345 : ContentPage
    {
        public ObservableCollection<string> Items { get; set; }

        public Issue12345()
        {
            InitializeComponent();
            
            Items = new ObservableCollection<string>
            {
                "Item 1",
                "Item 2",
                "Item 3"
            };
            
            BindingContext = this;
        }
    }
}
```

#### Step 2: Create NUnit Test

**File**: `src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue12345.cs`

```csharp
using NUnit.Framework;
using UITest.Appium;
using UITest.Core;

namespace Microsoft.Maui.TestCases.Tests.Issues
{
    public class Issue12345 : _IssuesUITest
    {
        public Issue12345(TestDevice device) : base(device)
        {
        }

        public override string Issue => "CollectionView RTL padding incorrect on iOS";

        [Test]
        [Category(UITestCategories.CollectionView)]
        public void CollectionViewRTLPaddingShouldBeCorrect()
        {
            // Wait for collection to load
            App.WaitForElement("TestCollection");
            
            // Verify the fix with screenshot
            // Screenshot will show RTL padding is correctly applied
            VerifyScreenshot();
        }
    }
}
```

**Key points for UI tests:**
- Use `[Issue]` attribute with issue number and description
- Add proper `AutomationId` to all interactive elements
- Use `VerifyScreenshot()` for visual validation
- Add to appropriate `[Category]` (CollectionView, Label, Entry, etc.)
- Keep test simple and focused on the specific issue

#### Step 3: Run Tests and Generate Screenshot Baselines

**Critical**: UI tests with `VerifyScreenshot()` require baseline screenshots to pass in CI.

**Generate baselines locally:**

**Android:**
```bash
# Build and deploy test app
dotnet build src/Controls/tests/TestCases.HostApp/Controls.TestCases.HostApp.csproj \
  -f net10.0-android -t:Run

# Run the specific test to generate screenshot
dotnet test src/Controls/tests/TestCases.Android.Tests/Controls.TestCases.Android.Tests.csproj \
  --filter "FullyQualifiedName~Issue12345"

# Screenshot saved to: src/Controls/tests/TestCases.Android.Tests/snapshots/
```

**iOS:**
```bash
# Find simulator UDID
UDID=$(xcrun simctl list devices available --json | jq -r '.devices | to_entries | map(select(.key | startswith("com.apple.CoreSimulator.SimRuntime.iOS"))) | map({key: .key, version: (.key | sub("com.apple.CoreSimulator.SimRuntime.iOS-"; "") | split("-") | map(tonumber)), devices: .value}) | sort_by(.version) | reverse | map(select(.devices | any(.name == "iPhone Xs"))) | first | .devices[] | select(.name == "iPhone Xs") | .udid')

# Build app
dotnet build src/Controls/tests/TestCases.HostApp/Controls.TestCases.HostApp.csproj -f net10.0-ios

# Boot and install
xcrun simctl boot $UDID 2>/dev/null || true
xcrun simctl install $UDID artifacts/bin/Controls.TestCases.HostApp/Debug/net10.0-ios/iossimulator-arm64/Controls.TestCases.HostApp.app

# Run test
dotnet test src/Controls/tests/TestCases.iOS.Tests/Controls.TestCases.iOS.Tests.csproj \
  --filter "FullyQualifiedName~Issue12345"

# Screenshot saved to: src/Controls/tests/TestCases.iOS.Tests/snapshots/ios/
```

**After running tests:**
1. Locate the generated screenshot file (named after your test method)
2. Verify the screenshot shows the CORRECT/FIXED behavior
3. Commit the screenshot file with your PR:
   ```bash
   git add src/Controls/tests/TestCases.*.Tests/snapshots/
   git commit -m "Add UI test screenshots for Issue12345"
   ```

**Important**: If you don't include screenshot baselines, CI will fail when it tries to verify screenshots.

See `.github/instructions/uitests.instructions.md` for comprehensive UI testing guidance.

### Code Formatting

**Before committing, format the code:**

```bash
# Format the entire solution
dotnet format Microsoft.Maui.sln --no-restore --exclude Templates/src --exclude-diagnostics CA1822

# Verify no formatting issues
dotnet format Microsoft.Maui.sln --verify-no-changes --no-restore --exclude Templates/src
```

### Creating the PR

**Step-by-step process:**

1. **Clean up unrelated changes before committing:**
   
   **Remove changes to files not related to your fix:**
   - Revert any modifications to `MainPage.xaml` and `MainPage.xaml.cs` (used for local testing)
   - Revert changes to any other test/sample files you used during reproduction
   - Remove any debug logging or temporary instrumentation added during investigation
   
   **Check what's staged:**
   ```bash
   git status
   git diff
   ```
   
   **Revert specific unrelated files:**
   ```bash
   # Revert Sandbox MainPage files
   git checkout -- src/Controls/samples/Controls.Sample.Sandbox/MainPage.xaml
   git checkout -- src/Controls/samples/Controls.Sample.Sandbox/MainPage.xaml.cs
   
   # Or use git restore (newer syntax)
   git restore src/Controls/samples/Controls.Sample.Sandbox/MainPage.xaml
   git restore src/Controls/samples/Controls.Sample.Sandbox/MainPage.xaml.cs
   ```
   
   **Your PR should only include:**
   - ✅ The actual fix (modified source files)
   - ✅ UI test files (TestCases.HostApp/Issues/IssueXXXXX.*)
   - ✅ NUnit test files (TestCases.Shared.Tests/Tests/Issues/IssueXXXXX.cs)
   - ✅ Screenshot baselines (if using VerifyScreenshot)
   - ✅ PublicAPI.Unshipped.txt updates (if you added public APIs)
   - ❌ NOT: MainPage.xaml/cs changes from Sandbox testing
   - ❌ NOT: Temporary debug code or console logging
   - ❌ NOT: Auto-generated files (cgmanifest.json, templatestrings.json)

2. **Commit your changes:**
   ```bash
   git add .
   git commit -m "[Issue-Resolver] Fix #12345 - CollectionView RTL padding"
   ```

3. **Push to your fork:**
   ```bash
   git push origin fix-issue-12345
   ```

4. **Open PR on GitHub:**
   - Navigate to https://github.com/dotnet/maui
   - Click "Pull requests" → "New pull request"
   - Select your branch
   - Fill in the complete PR description and title template

5. **Add required note at top of description:**
   ```markdown
   <!-- Please let the below note in for people that find this PR -->
   > [!NOTE]
   > Are you waiting for the changes in this PR to be merged?
   > It would be very helpful if you could [test the resulting artifacts](https://github.com/dotnet/maui/wiki/Testing-PR-Builds) from this PR and let us know in a comment if this change resolves your issue. Thank you!
   ```

6. **Link the issue:**
   - Use `Fixes #12345` in PR description
   - GitHub will automatically link and close the issue when PR merges

7. **Request review:**
   - Tag appropriate maintainers if known
   - Wait for CI checks to pass
   - Respond to review feedback promptly

## PR Checklist

**Before submitting, verify:**

- [ ] PR title follows format: `[Issue-Resolver] Fix #XXXXX - <Description>`
- [ ] PR description is complete with all required sections
- [ ] Required note added at top of PR description
- [ ] Issue is linked with `Fixes #XXXXX`
- [ ] Root cause explained clearly
- [ ] Solution approach documented
- [ ] Before/after test results included
- [ ] Edge cases documented
- [ ] All affected platforms tested
- [ ] UI tests created (TestCases.HostApp + Shared.Tests)
- [ ] Code formatted with `dotnet format`
- [ ] No breaking changes (or justified and documented)
- [ ] PublicAPI.Unshipped.txt updated if public APIs changed
- [ ] XML documentation added for new public APIs
- [ ] No auto-generated files committed (cgmanifest.json, templatestrings.json)

## After PR Submission

**Monitor your PR:**

1. **Watch for CI failures:**
   - Build errors → Fix and push update
   - Test failures → Investigate and fix
   - Format issues → Run `dotnet format` and push

2. **Respond to review comments:**
   - Address feedback promptly
   - Explain your reasoning when needed
   - Make requested changes
   - Re-request review after updates

3. **Update based on feedback:**
   - Make code changes as requested
   - Add additional tests if needed
   - Update documentation
   - Push updates to the same branch

4. **Communicate with issue reporter:**
   - Let them know PR is submitted
   - Ask them to test the PR build when available
   - Thank them for reporting the issue

## Common PR Mistakes to Avoid

❌ **Incomplete PR description**
- Missing root cause explanation
- No before/after test results
- Missing edge case documentation

❌ **Missing tests**
- No UI tests added
- Only tested happy path
- Didn't test on all affected platforms

❌ **Too many changes**
- Unrelated refactoring included
- Multiple issues fixed in one PR
- Changed code formatting throughout unrelated files

❌ **Not linking the issue**
- Forgot `Fixes #XXXXX` in description
- Issue not automatically closed when merged

❌ **Breaking changes without justification**
- Changed public API behavior without explanation
- No migration guidance provided

❌ **Ignoring CI failures**
- Submitted PR with known build errors
- Didn't check test results

## Example of Excellent PR

**Title**: `[Issue-Resolver] Fix #12345 - CollectionView RTL padding incorrect on iOS`

**Description**: Complete, with all sections filled in, root cause explained, solution documented, comprehensive testing results, UI tests added

**Changes**: Minimal, focused only on fixing the specific issue

**Tests**: Both HostApp page and NUnit test added, edge cases covered

**Documentation**: XML docs added for any new public methods

**Result**: Reviewers can quickly understand the issue, verify the fix is correct, and approve with confidence

This is what every issue-resolver PR should look like.
