# Test Type Decision Guide for .NET MAUI PRs

## Quick Decision Flowchart

```
START: User asks to test/validate a PR
    ↓
[1] Does PR modify control property behavior/logic?
    YES → Unit Tests (SliderTests.cs, StepperTests.cs, etc.)
    NO  → Continue
    ↓
[2] Does PR modify visual appearance/layout/rendering?
    YES → UI Tests (TestCases.HostApp + TestCases.Shared.Tests)
    NO  → Continue
    ↓
[3] Does PR modify platform-specific handlers?
    YES → UI Tests (to verify cross-platform behavior)
    NO  → Continue
    ↓
[4] Does PR add new control/feature?
    YES → Both Unit Tests + UI Tests
    NO  → Continue
    ↓
[5] Does PR fix a race condition/timing issue?
    YES → UI Tests (to verify under real conditions)
    NO  → Continue
    ↓
[6] Does PR modify XAML parsing/binding?
    YES → Check if behavior can be tested without UI
        - Can test programmatically? → Unit Tests
        - Needs visual verification? → UI Tests
    NO  → Sandbox for manual validation only
```

---

## Decision Matrix

| Scenario | Test Type | Example | Location |
|----------|-----------|---------|----------|
| **Property order independence** | ✅ Unit Tests | Slider Min/Max/Value order | `Controls.Core.UnitTests/SliderTests.cs` |
| **Value clamping logic** | ✅ Unit Tests | Stepper value constraints | `Controls.Core.UnitTests/StepperUnitTests.cs` |
| **Property coercion** | ✅ Unit Tests | Range validation | `Controls.Core.UnitTests/` |
| **Event firing order** | ✅ Unit Tests | PropertyChanged sequence | `Controls.Core.UnitTests/` |
| **Collection manipulation** | ✅ Unit Tests | Add/Remove/Clear items | `Controls.Core.UnitTests/` |
| **Layout measurement** | ❌ UI Tests | SafeArea padding | `TestCases.HostApp/Issues/` |
| **Visual rendering** | ❌ UI Tests | Border thickness | `TestCases.HostApp/Issues/` |
| **Platform handlers** | ❌ UI Tests | iOS vs Android behavior | `TestCases.HostApp/Issues/` |
| **Navigation** | ❌ UI Tests | Shell navigation | `TestCases.HostApp/Issues/` |
| **Gestures** | ❌ UI Tests | Tap/swipe interactions | `TestCases.HostApp/Issues/` |
| **Race conditions** | ❌ UI Tests | Timing-sensitive bugs | `TestCases.HostApp/Issues/` |

---

## Detailed Decision Rules

### ✅ Write Unit Tests When:

**Control Logic/Behavior** (No UI needed):
- Property value changes (setters/getters)
- Property validation/coercion
- Property order independence
- Event firing order
- Collection manipulation (add/remove/clear)
- State management
- Data binding (non-visual)

**Why Unit Tests?**
- ✅ Fast execution (milliseconds)
- ✅ No platform-specific setup needed
- ✅ Easy to test all permutations
- ✅ Runs in CI without simulators/emulators
- ✅ Debuggable with simple breakpoints

**Example - PR #32939 (Slider/Stepper property order)**:
```csharp
// This tests LOGIC, not visual appearance
[Test]
public void SetProperties_MinValueMax_Order()
{
    var slider = new Slider();
    slider.Minimum = 10;
    slider.Value = 50;
    slider.Maximum = 100;
    
    Assert.Equal(50, slider.Value); // No UI needed!
}
```

**Location**: `src/Controls/tests/Core.UnitTests/`
- `SliderTests.cs`
- `StepperUnitTests.cs`
- `ButtonUnitTests.cs`
- `EntryUnitTests.cs`
- etc.

---

### ❌ Write UI Tests When:

**Visual/Interactive/Platform-Specific** (UI required):
- Layout measurement and positioning
- Visual rendering (colors, borders, shadows)
- Platform-specific handler behavior
- Navigation and routing
- Gestures and touch interactions
- Keyboard input and focus
- Accessibility
- Race conditions (timing-sensitive)
- Screenshot verification

**Why UI Tests?**
- ✅ Verifies actual rendered output
- ✅ Tests platform-specific implementations
- ✅ Catches visual regressions
- ✅ Tests real user interactions

**Example - SafeArea PR**:
```csharp
// This tests VISUAL LAYOUT, needs UI
[Test]
public void SafeAreaPaddingAppliedCorrectly()
{
    App.WaitForElement("ContentGrid");
    var rect = App.FindElement("ContentGrid").GetRect();
    
    // Verify visual positioning on screen
    Assert.That(rect.Y, Is.GreaterThan(0)); // Needs actual rendering!
}
```

**Location**: 
- `src/Controls/tests/TestCases.HostApp/Issues/IssueXXXXX.xaml[.cs]`
- `src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/IssueXXXXX.cs`

---

### 🔄 Both Unit Tests + UI Tests When:

**New Control or Major Feature**:
- Unit tests for property logic
- UI tests for visual behavior and interactions

**Example - New DatePicker Control**:
- Unit tests: Date validation, property coercion, event firing
- UI tests: Calendar popup, date selection, keyboard input

---

## How to Recognize from PR Changes

### 🔍 Clues That Suggest Unit Tests:

Look at the **modified files** in the PR:

1. **Changes to control source files** (without handler changes):
   ```
   src/Controls/src/Core/Slider/Slider.cs
   src/Controls/src/Core/Stepper/Stepper.cs
   ```
   → Property logic changes → **Unit tests**

2. **Changes to BindableProperty definitions**:
   ```csharp
   public static readonly BindableProperty MinimumProperty = 
       BindableProperty.Create(..., coerceValue: ..., propertyChanged: ...);
   ```
   → Property behavior → **Unit tests**

3. **PR already includes unit tests**:
   ```
   src/Controls/tests/Core.UnitTests/SliderTests.cs  [+112 lines]
   ```
   → Author already chose unit tests → **Follow their approach**

4. **No visual/layout keywords in PR description**:
   - No mention of "rendering", "visual", "layout", "padding"
   - Focus on "property order", "value preservation", "event sequence"
   → **Unit tests**

### 🔍 Clues That Suggest UI Tests:

1. **Changes to platform handlers**:
   ```
   src/Core/src/Handlers/Slider/SliderHandler.Android.cs
   src/Core/src/Handlers/Slider/SliderHandler.iOS.cs
   ```
   → Platform-specific behavior → **UI tests**

2. **Changes to layout/measurement code**:
   ```
   src/Core/src/Layouts/LayoutManager.cs
   src/Controls/src/Core/Layout/LayoutExtensions.cs
   ```
   → Visual positioning → **UI tests**

3. **PR includes UI test files**:
   ```
   src/Controls/tests/TestCases.HostApp/Issues/Issue12345.xaml
   src/Controls/tests/TestCases.Shared.Tests/Tests/Issues/Issue12345.cs
   ```
   → Author chose UI tests → **Follow their approach**

4. **Visual/layout keywords in PR description**:
   - "padding", "margin", "layout", "rendering", "visual", "SafeArea"
   - "tap", "gesture", "navigation", "keyboard"
   → **UI tests**

---

## Case Study: PR #32939 Analysis

**PR #32939**: Fix Slider and Stepper property order independence

**Modified Files**:
```
✅ src/Controls/src/Core/Slider/Slider.cs          [Property logic]
✅ src/Controls/src/Core/Stepper/Stepper.cs        [Property logic]
✅ src/Controls/tests/Core.UnitTests/SliderTests.cs [+112 tests]
✅ src/Controls/tests/Core.UnitTests/StepperUnitTests.cs [+139 tests]
```

**PR Description Keywords**:
- "property order independence"
- "Value property preserved"
- "property initialization order"
- "binding evaluation timing"

**Decision Signals**:
- ✅ Only touches control source files (no handlers)
- ✅ Changes BindableProperty definitions (coerceValue → propertyChanged)
- ✅ PR author already included comprehensive unit tests (98 tests!)
- ✅ No visual/layout changes mentioned
- ✅ Keywords focus on property behavior, not visual appearance

**Correct Test Type**: ✅ **Unit Tests**

**What I Should Have Done**:
1. Notice PR already includes unit tests
2. Add additional unit test scenarios if needed
3. Run existing unit tests to verify they pass
4. Use Sandbox ONLY for manual exploratory testing (optional)

**What I Did Wrong**:
1. ❌ Created Sandbox test scenario with UI
2. ❌ Wrote Appium automation scripts
3. ❌ Focused on visual validation instead of logic validation

**Why Unit Tests Were Better**:
- ✅ PR author already wrote 98 comprehensive unit tests
- ✅ Property order is pure logic, no UI needed
- ✅ Unit tests are faster and more reliable
- ✅ Unit tests already cover all 6 permutations
- ✅ Unit tests run in CI without device setup

---

## Updated Decision Process

When user asks to "test/validate PR #XXXXX":

### Step 1: Analyze PR Changes
```bash
# Fetch PR info
gh pr view XXXXX

# Check modified files
gh pr diff XXXXX --name-only
```

### Step 2: Identify Test Type

Ask yourself:
1. **Does PR already include tests?**
   - Unit tests in `Core.UnitTests/`? → Add more unit tests if needed
   - UI tests in `TestCases.HostApp/`? → Add more UI tests if needed
   - No tests? → Continue to step 2

2. **Can the behavior be tested without UI?**
   - Property changes? → Unit tests
   - Event firing? → Unit tests
   - Value validation? → Unit tests
   - Continue to step 3

3. **Does it require visual verification?**
   - Layout positioning? → UI tests
   - Rendering/appearance? → UI tests
   - Platform handlers? → UI tests
   - Continue to step 4

4. **When in doubt, ask the user:**
   ```markdown
   I notice PR #XXXXX modifies [control property logic / visual layout / etc].
   
   Would you like me to:
   1. Add unit tests to validate the logic (faster, no device needed)
   2. Create UI tests to verify visual behavior (requires device/simulator)
   3. Create Sandbox scenario for manual testing
   
   The PR author already included [unit tests / UI tests / no tests].
   I recommend [option X] because [reason].
   ```

### Step 3: Execute Test Strategy

**If Unit Tests**:
1. Add tests to `src/Controls/tests/Core.UnitTests/[Control]Tests.cs`
2. Follow existing test patterns in the file
3. Run tests: `dotnet test --filter "FullyQualifiedName~[Control]"`
4. Report results

**If UI Tests**:
1. Create `TestCases.HostApp/Issues/IssueXXXXX.xaml[.cs]`
2. Create `TestCases.Shared.Tests/Tests/Issues/IssueXXXXX.cs`
3. Follow patterns from `.github/instructions/uitests.instructions.md`
4. Report results

**If Sandbox (Manual Testing)**:
1. Modify `Controls.Sample.Sandbox/MainPage.xaml[.cs]`
2. Run `BuildAndRunSandbox.ps1`
3. Report observations

---

## Key Takeaways

### Rule #1: Follow the PR Author's Lead
If PR includes unit tests → Add more unit tests  
If PR includes UI tests → Add more UI tests  
If PR includes no tests → Analyze and decide

### Rule #2: Prefer Unit Tests When Possible
If behavior can be tested without UI → Unit tests  
Only use UI tests when visual/interactive verification is required

### Rule #3: Sandbox is for Exploration
Use Sandbox for:
- Manual exploratory testing
- Quick validation before writing formal tests
- Scenarios that don't need automated tests

Don't use Sandbox as a replacement for unit or UI tests.

---

## Summary

**PR #32939 Lesson Learned**:
- ✅ Property order logic = Unit tests
- ❌ Don't create Sandbox + Appium when unit tests already exist
- ✅ Follow PR author's test approach (they wrote 98 unit tests!)
- ✅ Sandbox is for exploration, not validation

**Next Time**:
1. Check if PR already includes tests
2. Analyze what's being tested (logic vs visual)
3. Choose appropriate test type
4. Don't default to Sandbox/UI tests for everything
