# Wordzzle on .NET 11 / MAUI 11 RC2

A runnable, offline migration of the daily word-game loop from
[kubaflo/Wordzzle](https://github.com/kubaflo/Wordzzle), plus a dedicated MAUI 11
hackathon lab. **The screenshots and videos below show real .NET 11 Android
and iOS apps, not mockups or the earlier .NET 10 build.**

Android uses the exact event RC2 stack. **iOS works with Xcode 26.6**, using
the opt-in .NET 11 Preview 6 SDK/Apple workload profile and an iOS-26.5-compatible
MAUI 11 RC2 package. The iOS profile is not the exact event SDK stack.

The sample pins the event SDK and workload in [global.json](global.json).
Its local `Directory.Build.props` and `Directory.Build.targets` deliberately
isolate it from this framework checkout's .NET 10 source-build configuration.
It consumes MAUI 11 packages, not this checkout's MAUI assemblies.

## See the app working

### VS Code F5: launch and counter verified

On **2026-09-30**, F5 in the actual Wordzzle VS Code window launched the
Android RC2 app under the **CoreCLR mobile debugger**. The editor reported
`Debugging started.`, `Call Stack - 1 active session`, and
`C# Hot Reload is available`. The IDE's build/deploy task exited successfully;
this was not the earlier build-only attempt with `skipDebug=true`.

On the selected API30 emulator, opening the lab and tapping its existing
`Count +2` button produced **Counter: 2**, with **PID 10140** and page session
**`c962e370`**. No source edit or Hot Reload was involved in that tap.

**The hackathon debugger lane is still incomplete.** The desktop locked
before the breakpoint, variable inspection, stepping, repeated launches,
and editor Hot Reload checks. Desktop automation correctly refused further
input. There is no paused-debugger screenshot or IDE Hot Reload recording
yet; the device capture below is not presented as either.

**Actual VS Code device-picker capture, before the successful launch:**

<img src="Screenshots/vscode-02-device-picker.jpg" width="1000" alt="Actual VS Code MAUI device picker during the initial startup-selection failure, before successful debugging" />

This preserves the initial picker failure rather than hiding it: selecting
the running device did not persist the active target/platform. An explicit,
local launch configuration subsequently worked. The pictured highlighted
device is not the emulator used for the successful launch.

**Actual F5-launched app after the counter tap:**

<img src="Screenshots/vscode-03-f5-android-counter.png" width="300" alt="F5-launched Wordzzle Android app with counter 2 and session c962e370" />

<details>
<summary>Earlier source-only VS Code capture</summary>

<img src="Screenshots/vscode-01-editor.jpg" width="1000" alt="Earlier actual VS Code window with Wordzzle and MainPage.xaml.cs open, without an active debugger" />

This earlier picture is source view only. Its three diagnostics are historical;
after the project-system and restore recovery, the editor showed zero errors,
zero warnings, and one information item.

</details>

### Fresh RC2 template

The isolated-hive `dotnet new maui` project was also built and launched on the
API30 emulator. Its untouched counter changed from `Click me` to
`Clicked 1 time`, then **`Clicked 2 times`**. This is a separate template app,
not the Wordzzle migration and not an IDE-debugging test. The build used the
exact RC2 SDK, `net11.0-android` / `android-arm64`, and
`EmbedAssembliesIntoApk=true`; deployment and interaction used device tools.

<img src="Screenshots/template-01-rc2-counter.png" width="300" alt="Fresh MAUI 11 RC2 template running on Android with its counter showing Clicked 2 times" />

### Android RC2

Captured on Android 11 / API 30, running a `net11.0-android` CoreCLR Debug build.
Most images come directly from the real DevFlow MCP screenshot tool; native
dialogs require a device screenshot.

| Daily puzzle | A submitted guess | Progress after a cold restart |
| --- | --- | --- |
| <img src="Screenshots/01-net11-daily.png" width="240" alt="Wordzzle daily game running on .NET 11" /> | <img src="Screenshots/02-first-guess.png" width="240" alt="CRANE scored against the daily answer" /> | <img src="Screenshots/03-restored-progress.png" width="240" alt="CRANE and partially typed CLO restored after restarting" /> |

| Completed puzzle | Empty-input validation | Greeting and C# XAML expression |
| --- | --- | --- |
| <img src="Screenshots/04-solved.png" width="240" alt="CLOUD wins in two guesses" /> | <img src="Screenshots/07-empty-validation.png" width="240" alt="Empty input displays Please enter your name" /> | <img src="Screenshots/08-greeting.png" width="240" alt="Hello Maui tester and Characters 11" /> |

### Real Hot Reload, not a rebuild

These three captures share **Android PID 6418 and page session `f9a643b3`**.
The running counter survives the XAML edit, then increases from 3 to 5 after
changing the existing C# handler from `+= 1` to `+= 2`. Navigation is retained.

| Before editing | XAML source Hot Reload | C# handler Hot Reload |
| --- | --- | --- |
| <img src="Screenshots/10-before-hotreload.png" width="240" alt="Original label, counter 3, session f9a643b3" /> | <img src="Screenshots/11-xaml-hotreload.png" width="240" alt="Updated live in green, counter still 3, same session" /> | <img src="Screenshots/12-csharp-hotreload.png" width="240" alt="Count plus 2 produces counter 5 in the same session" /> |

**This is verified `dotnet watch` Hot Reload. It is not evidence of VS Code F5,
the VS Code flame button, or a MAUI chat-agent session.** Those editor-specific
Hot Reload and agent lanes remain unverified; the separate F5 observation is
above and the full status matrix is below.

### Watch the recordings

| Recording | What to watch |
| --- | --- |
| **[Play XAML Hot Reload (20 seconds)](Videos/01-xaml-hotreload.mp4)** | Around 6 seconds, the label and button become `Updated live`; the label turns green and grows. Counter stays 3 and session stays `f9a643b3`. |
| **[Play C# Hot Reload (23 seconds)](Videos/02-csharp-hotreload.mp4)** | Around 4 seconds, `Count +1` becomes `Count +2`. Around 17 seconds, tapping it changes the existing counter from 3 to 5, with the same session. |

These are actual Android screen recordings, with only leading/trailing idle
time removed and H.264 re-encoding for playback compatibility. Playback speed
is unchanged. They show the device, **not the VS Code desktop**; the edits were
applied through `dotnet watch` after VS Code input automation failed. They are
not screenshot slideshows and are not labeled as editor Hot Reload evidence.

| Local statistics | Recovery after an intentional compiler error |
| --- | --- |
| <img src="Screenshots/05-statistics.png" width="240" alt="Native statistics dialog shows one game, one win, and two guesses" /> | <img src="Screenshots/13-error-recovery.png" width="240" alt="Counter 10 after fixing the intentional syntax error" /> |

### iOS without Xcode 27

Captured on **iPhone 17 / iOS 26.5**, running `net11.0-ios` / CoreCLR with
Xcode **26.6**. No Xcode installation, global Xcode switch, .NET 10 fallback,
or version-check bypass was used.

| Solved game restored after relaunch | Empty-input validation | Greeting and live expression |
| --- | --- | --- |
| <img src="Screenshots/ios-02-solved.png" width="240" alt="iOS restores CRANE and CLOUD, solved in two guesses" /> | <img src="Screenshots/ios-06-empty-validation.png" width="240" alt="iOS displays Please enter your name" /> | <img src="Screenshots/ios-07-greeting.png" width="240" alt="iOS displays Hello Maui tester and Characters 11" /> |

The following captures share **iOS PID 12297 and page session `68811798`**.
XAML text, color, font size and button text changed without leaving the page.
The existing counter stayed 3, then became 5 after the C# handler changed
from `+= 1` to `+= 2` and the button was tapped.

| Before editing | XAML source Hot Reload | C# handler Hot Reload |
| --- | --- | --- |
| <img src="Screenshots/ios-03-before-hotreload.png" width="240" alt="iOS counter 3 before Hot Reload, session 68811798" /> | <img src="Screenshots/ios-04-xaml-hotreload.png" width="240" alt="iOS label turns green and grows while counter and session remain unchanged" /> | <img src="Screenshots/ios-05-csharp-hotreload.png" width="240" alt="iOS counter becomes 5 without restarting" /> |

| Recording | What to watch |
| --- | --- |
| **[Play iOS XAML Hot Reload (16 seconds)](Videos/03-ios-xaml-hotreload.mp4)** | The label and button change to `Updated live`; the label becomes green and larger. Counter remains 3. |
| **[Play iOS C# Hot Reload (22 seconds)](Videos/04-ios-csharp-hotreload.mp4)** | `Count +1` changes to `Count +2`, then counter 3 becomes 5 near 17 seconds. The session ID stays unchanged. |

These are native simulator screen recordings, converted to H.264 / 30 fps
without speeding up the edits. The C# clip holds its final captured frame for
five seconds so the result is readable. They show **CLI `dotnet watch`**, not
the VS Code desktop, debugger, flame button or chat agent.

An intentional `_state.ClickCount += ;` also produced CS1525 on iOS. The old
valid handler still worked (5 -> 7), and correcting the source allowed another
tap (7 -> 9), without a process or page restart
([recovery capture](Screenshots/ios-08-error-recovery.png)).
Restoring the already-applied valid source reported
`No managed code changes to apply`; it did not silently rebuild/relaunch.

## Migration scope

Implemented: four-to-seven-letter English daily puzzles, six attempts, a
tappable keyboard, count-aware duplicate-letter scoring, local per-puzzle
progress, win statistics, help, and length selection. The puzzle date follows
the original noon-UTC boundary when the game page is constructed.

This is **not full feature parity** with the Xamarin.Forms application.
The original calendar, other language dictionaries, Firebase battles,
leaderboards, account synchronization, ads, audio, and notifications are not
migrated. The English dictionaries here are small offline sample lists.
There is no automatic noon rollover while an existing page stays open, and
compact/landscape layouts and live theme switching have not been validated.

The lab contains the counter/session marker, experimental C# XAML expressions,
the hackathon button, a name field with empty-input validation, and a scroll
target. Stable `AutomationId` values allow real runtime inspection.

## Run the exact RC2 build

Run commands **inside this sample directory**, not at the repository root:

```bash
cd src/Controls/samples/Wordzzle
```

Install the SDK version in `global.json` using the event's approved distribution.
For an isolated install, extract the SDK into the ignored `.dotnet/` directory:

```bash
export DOTNET_ROOT="$PWD/.dotnet"
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version
# 11.0.100-rc.2.26475.136
```

An existing installation of that exact SDK also works through the `$host$`
fallback in `global.json`. There is deliberately no version roll-forward.

The repository NuGet configuration contains only public Microsoft feeds.
For the exact event runtime/workload packages, use an **external, authenticated
NuGet configuration** supplied by the organizer. A public-only restore failed
for some pinned RC2 packages on the validation host. Do not commit credentials,
private feed URLs, or downloaded SDK archives.

```bash
export WORDZZLE_NUGET_CONFIG=/absolute/path/to/your/event-NuGet.config

dotnet workload install maui --version 11.0.100-rc.2.26478.2 \
  --configfile "$WORDZZLE_NUGET_CONFIG" --interactive

export DEVICE=emulator-5582 # replace with your booted device's adb serial
dotnet run --project Wordzzle.csproj --framework net11.0-android \
  --device "$DEVICE" --runtime android-arm64 \
  --property:TargetFrameworks=net11.0-android \
  --property:EmbedAssembliesIntoApk=true \
  --property:RestoreConfigFile="$WORDZZLE_NUGET_CONFIG"
```

Use the RID appropriate to your device. Embedding the assemblies makes the
Debug APK self-contained for deployment; manually sideloading the initial
fast-deployment APK without its assemblies failed to initialize CoreCLR.

The project also declares iOS, Mac Catalyst, and Windows targets. The exact
event RC2 iOS workload requires Xcode 27.0, whereas this host uses Xcode 26.6.
The working alternate iOS profile is documented below. Mac Catalyst and
Windows were not tested.

### Hot Reload

```bash
dotnet watch --project Wordzzle.csproj --framework net11.0-android \
  --device "$DEVICE" --runtime android-arm64 \
  --property:TargetFrameworks=net11.0-android \
  --property:EmbedAssembliesIntoApk=true \
  --property:RestoreConfigFile="$WORDZZLE_NUGET_CONFIG"
```

Use `--property:...`, not `-p:...`, with this watch build: combining the latter
with `--project` produced `Cannot specify both '--project' and '-p' options.`

Open **MAUI 11 hackathon lab**, tap the counter, and note both its value and
session ID. The committed lab is left in the demonstrated `Updated live` /
`Count +2` state. To reproduce the original sequence, first restore the initial
values below before launching watch.

| File | Before | After |
| --- | --- | --- |
| `HackathonPage.xaml`, `LiveLabel` | `Text="Ready for live edits"`, `FontSize="28"` | `Text="Updated live"`, `TextColor="#538D4E"`, `FontSize="30"` |
| `HackathonPage.xaml`, `HackathonButton` | `Text="Hackathon check"` | `Text="Updated live"` |
| `HackathonPage.xaml.cs`, `OnCounterClicked` | `_state.ClickCount += 1;` | `_state.ClickCount += 2;` |
| `HackathonPage.xaml`, `CounterButton` | `Text="Count +1"` | `Text="Count +2"` |

In the earlier error-recovery run (PID 4332, session `98947ca6`), reverting the
source handler to `+= 1` produced counter 6;
reapplying `+= 2` produced 8. This was source revert/reapply, not VS Code
keyboard Undo/Redo. An intentional `_state.ClickCount += ;` then produced:

```text
Unable to apply changes due to compilation errors.
HackathonPage.xaml.cs(15,30): error CS1525: Invalid expression term ';'
```

The app remained usable at counter 8. After correcting the source, the next
tap produced 10 with the same process and session
([recovery screenshot](Screenshots/13-error-recovery.png)). Reverting to the
already-applied valid code reported `No managed code changes to apply`;
no restart was used to hide the error.

### VS Code and DevFlow

Open this sample as its own VS Code folder. Extension recommendations and
diagnostic settings are in `.vscode/`. On the validation host, C# Dev Kit
initially derived the runtime root from an unnormalized Homebrew `dotnet`
symlink, and its project-system host repeatedly exited 131. The workspace
setting `dotnet.useLegacyDotnetResolution=true` resolved the real installation
directory. Reloading the Wordzzle window then loaded C# Dev Kit **3.40.210**.
Other editor windows were not restarted.

Restore the **whole project**, not just the ARM64 Android target, using the
external event NuGet configuration:

```bash
dotnet restore Wordzzle.csproj --configfile "$WORDZZLE_NUGET_CONFIG"
```

The editor's generated solution also needed the pinned Android x64 and
Mac Catalyst ARM64 runtime packages. Restoring them resolved the editor's
missing-package diagnostics; this does not mean those platforms were run.

With MAUI extension **11.0.24**, both a relative `project` path and
`${workspaceFolder}/Wordzzle.csproj` failed with
`Debugging canceled: startup project not found.` The observed provider looks
up the project before variable substitution. Omitting the field exposed only
the test project in the startup picker, and device selection did not persist.
For this extension build, create an **ignored, machine-local**
`.vscode/launch.json`, replacing the example path and emulator serial:

```json
{
  "version": "0.2.0",
  "configurations": [
    {
      "name": "Wordzzle - Android RC2",
      "type": "maui",
      "request": "launch",
      "preLaunchTask": "maui: Build",
      "project": "/absolute/path/to/Wordzzle/Wordzzle.csproj",
      "targetFramework": "net11.0-android",
      "platform": "android",
      "device": "emulator-5554",
      "configuration": "Debug"
    }
  ]
}
```

Use the booted device's `adb devices` serial. The workspace enables
`maui.configuration.useLaunchJsonConfigurations` so these explicit settings
are used. This profile was exercised with F5; a portable variable-based
profile is **not** claimed to work with the observed provider.

Enable the following **user settings**, which are machine-scoped:

```json
{
  "csharp.experimental.debug.hotReload": true,
  "csharp.debug.hotReloadOnSave": true,
  "csharp.debug.hotReloadVerbosity": "diagnostic"
}
```

These settings were written on the validation host and the active debugger
reported `C# Hot Reload is available`. Applying and verifying an editor edit
is still outstanding; availability alone is not a Hot Reload pass.

`MauiProgram.cs` contains `#if MAUI_DEVFLOW` guarded startup. Ordinary builds
do not permanently reference the agent package. The MAUI extension can inject
its Debug-only package/define through `MauiDevFlow.targets`; plain F5 does not
automatically enable DevFlow.

For the recorded direct-tool experiments, the launch also supplied:

```text
--property:MauiDevFlowEnabled=true
--property:MauiDevFlowAgentVersion=0.1.0-preview.12.26421.1
--property:CustomAfterMicrosoftCommonTargets=<installed-extension>/dist/resources/MSBuild/MauiDevFlow.targets
```

Broker discovery initially returned no agents despite the device HTTP server
listening on port 9223. `adb -s "$DEVICE" forward tcp:9223 tcp:9223` and an
explicit agent port allowed the extension's real `devflow mcp` server to be
tested through stdio JSON-RPC. This is **actual MCP**, but not a claim that
VS Code Chat discovered or selected these tools. Use only on a local,
trusted development device; keep diagnostic ports off public interfaces.

`maui_set_property` changed `LiveLabel.Text` to
`Memory only - not Hot Reload`. A correct assertion passed; the intentional
wrong assertion returned:

```text
FAIL: Text expected "deliberately wrong" but got "Memory only - not Hot Reload"
```

After a fresh launch, the source value `Ready for live edits` returned.
The [memory-only capture](Screenshots/09-memory-property.png) is deliberately
separate from the source Hot Reload evidence.

## Run iOS with Xcode 26.6

Use [ios-xcode26/global.json](ios-xcode26/global.json) from its own directory.
It selects the already-installed .NET 11 Preview 6 SDK and Apple workload set;
the parent directory's Android RC2 pins are unchanged.

```bash
cd src/Controls/samples/Wordzzle/ios-xcode26

# Point to the installation containing the exact Preview 6 SDK.
export DOTNET_ROOT=/usr/local/share/dotnet
export PATH="$DOTNET_ROOT:$PATH"
dotnet --version
# 11.0.100-preview.6.26359.118

# Only if this workload set is not already installed:
dotnet workload install maui-ios --version 11.0.100-preview.6.26364.2 \
  --configfile ../NuGet.config

export IOS_DEVICE=5BA96735-B558-4355-B16A-B41143C6718A # replace with your booted simulator UUID
dotnet watch --project ../Wordzzle.csproj --framework net11.0-ios \
  --device "$IOS_DEVICE" --runtime iossimulator-arm64 \
  --property:TargetFrameworks=net11.0-ios \
  --property:RestoreConfigFile="$PWD/../NuGet.config"
```

The ordinary build command, without Hot Reload or DevFlow, is:

```bash
dotnet build ../Wordzzle.csproj -f net11.0-ios -r iossimulator-arm64 \
  -p:TargetFrameworks=net11.0-ios \
  -p:RestoreConfigFile="$PWD/../NuGet.config"
xcrun simctl install "$IOS_DEVICE" \
  ../bin/Debug/net11.0-ios/iossimulator-arm64/Wordzzle.app
xcrun simctl launch "$IOS_DEVICE" com.kubaflo.wordzzle
```

For the direct MCP experiments, append the three DevFlow properties from the
section above to `dotnet watch`. The iOS simulator's agent was reachable on
host loopback port 9223 without ADB forwarding.

Two narrowly scoped compatibility changes are included:

- With this exact SDK and `net11.0-ios`, the project selects
  `Microsoft.Maui.Controls` **11.0.0-rc.2.26474.65**, whose iOS assets target
  26.5. The event's newer **11.0.0-rc.2.26475.3** assets target iOS 27.0.
  Using Preview 6 MAUI alone launched successfully but did not visibly apply
  XAML Hot Reload; the compatible RC2 package did.
- `Directory.Build.targets` removes the erroneous executable quotes emitted
  by Preview 6's `ComputeMlaunchRunArguments` and supplies the project working
  directory. Otherwise the child-directory launch cannot start `mlaunch`
  correctly. This target does not modify the installed SDK or Xcode validation,
  and does not run for Android or the RC2 SDK.

This is an **empirically validated mixed SDK/framework profile**, not a claim
that the exact RC2 Apple workload supports Xcode 26.6. All framework TFMs,
runtime and Apple SDK remain net11; the public sample NuGet configuration was
sufficient on this host with its existing package cache.

## Hackathon coverage

Results are observations, not promises. **Blocked** and **Not run** are not Pass.

| Checklist area | Result | Evidence / remaining gap |
| --- | --- | --- |
| Create new project from template | Pass, CLI/device tools | Fresh isolated-hive RC2 template built and launched on Android; actual counter reached 2. [Template capture](Screenshots/template-01-rc2-counter.png). |
| Upgrade existing application | Partial | Daily-game subset runs on Android RC2 and the alternate net11 iOS profile; original service/feature parity remains out of scope. |
| Experimental C# XAML expressions | Pass | `EnablePreviewFeatures=true`; counter/session interpolation and `Name.Length` update on Android and iOS. |
| Android launch and counter | Pass, CLI and F5 | Actual VS Code F5/CoreCLR session on the selected API30 emulator; counter 2, PID 10140, session `c962e370`. Device capture and limitations above. |
| iOS launch and counter | Pass, CLI only | iPhone 17 / iOS 26.5, Xcode 26.6, compatible MAUI RC2 package; not the exact event SDK or F5 evidence. |
| F5, breakpoint, inspect, step, continue | Partial | F5 and counter interaction verified; desktop locked before breakpoint, variable inspection, step, and continue. No paused-debugger capture. |
| Three debugger stop/launch cycles | Not run | Three CLI cold launches passed (PIDs 8578, 8632, 8687) and the solved game persisted, but that does not validate debugger reconnects. |
| Second-device debug switch | Blocked | Desktop locked; Android and iOS CLI launches do not validate editor/debugger switching. |
| XAML Hot Reload: text/color/font | Pass, watch only | Android and iOS retain their PID/session and counter 3; actual properties become `Updated live`, `#538D4E`, and `30`. |
| C# Hot Reload: handler +1 to +2 | Pass, watch only | Counter 3 -> 5 on Android and iOS, without relaunch. |
| Several edits, revert/reapply | Pass, watch only | Counter 5 -> 6 -> 8; VS Code keyboard Undo/Redo remains untested. |
| Syntax error and recovery | Pass, watch only | CS1525 retained; Android counter 8 -> 10 after correction. iOS old handler remains usable (5 -> 7), then 7 -> 9 after correction, same session. |
| VS Code XAML/C# Hot Reload | Blocked | Debugger reports availability, but desktop locked before applying an edit; CLI evidence is not substituted. |
| MAUI agent recommendation/discovery | Not run | Recommendations configured; actual Chat behavior not observed. |
| Three ordered MAUI-agent prompts | Blocked | Desktop locked. Requested UI is implemented and exercised, but no MAUI chat prompt has been sent. |
| Agent device listing/switch, debug output/stop | Not run | Device/debug work used direct tools; editor agent unavailable. |
| Plain Copilot vs MAUI agent | Not run | No comparative chat session; no model substitution. |
| MCP tree/query/tap/fill/scroll/screenshot | Pass, direct MCP | Actual image returned; guesses, counter, scroll, empty-input error and `Maui tester` greeting verified. |
| MCP get/set/assert, including failure | Pass, direct MCP | Original/read value, memory-only mutation, positive and negative assertion recorded. |
| MCP relaunch/reconnect and reset | Pass, device launch | New app process inspected and source label restored; not MAUI debug-tool relaunch evidence. |
| Environment/deviations | Recorded | Exact versions below; logs retained locally, secrets not published. |
| Show-and-tell slides | Prepared | [Two-slide deck](Wordzzle-hackathon.pptx); no event upload, because the supplied destination was `TODO`. |
| Bug-report workflow | Partial | Related issues searched; sanitized failures below. No internal tracker destination/access or public issue submission claimed. |

The three prompts still needing a real **MAUI** agent chat are:

1. "Add a button labeled 'Hackathon check'. Launch the app on my selected device and verify the button is visible."
2. "Change that button's text to 'Updated live'. Use Hot Reload if supported and check the result."
3. "Add a name field and a Submit button. Empty input should show an error; entering 'Maui tester' should show a greeting. Test both cases."

## Recorded environment and first failures

CLI Hot Reload validation date: **2026-09-29 UTC**.
VS Code recovery, F5/counter and template launch: **2026-09-30 UTC**.

| Component | Actual value |
| --- | --- |
| Host | macOS 26.7 (`25G229`), Apple Silicon |
| Android SDK | `11.0.100-rc.2.26475.136` |
| Android workload set | `11.0.100-rc.2.26478.2` |
| Android MAUI | `11.0.0-rc.2.26475.3` |
| Android workload | `37.2.0-rc.2.84` |
| Runtime / configuration | CoreCLR, Debug, `android-arm64` |
| Running device | Android 11 / API30, 1080 x 1920, density 2.625 |
| Android toolchain | Target API37, build tools 36.0.0, JDK 21.0.8 |
| Exact-event iOS workload (blocked) | `27.0.12211-net11-rc.2`; requires Xcode 27.0 |
| Working iOS SDK / workload set | `11.0.100-preview.6.26359.118` / `11.0.100-preview.6.26364.2` |
| Working iOS Apple pack / MAUI | `26.5.11720-net11-p6` / `11.0.0-rc.2.26474.65` |
| Working iOS device / runtime | iPhone 17, iOS 26.5, CoreCLR, Debug, `iossimulator-arm64` |
| Selected Xcode | 26.6 (`17F113`), unchanged |
| VS Code | `1.139.0`, arm64, commit `2242ebbb54efeeb0129e08e919e7e8d43033cd83` |
| .NET MAUI extension | `11.0.24`; differs from checklist `1.17.266` |
| C# extension | `2.160.4` |
| C# Dev Kit | `3.40.210` loaded after window reload; earlier attempts used `3.40.204` |
| SDK selected by VS Code F5 | Sample-local `11.0.100-rc.2.26475.136`; IDE-generated build/deploy command verified |
| IDE Hot Reload settings | Experimental C# and apply-on-save enabled in user settings; available indicator observed, edit application untested |
| Copilot Chat / selected editor agent | Not verified; no successful editor chat session |
| XAML mode | `MauiXamlInflator=SourceGen`, `MauiXamlHotReload=SourceGen` |
| DevFlow agent | `0.1.0-preview.12.26421.1`, commit `78e85a5cb9c7a3efaaacf85d6176ce971f40b7d6` |

| First failure | Recovery / present status |
| --- | --- |
| Public-only exact-RC2 restore: unauthorized/missing runtime packages | Existing authorized event identity with an external NuGet config; no credentials persisted in the repository. |
| Direct nuget.org TLS: `Socket is not connected` | Used Microsoft's public NuGet mirror. |
| iOS: `This version of .NET for iOS (27.0.12211-net11-rc.2) requires Xcode 27.0. The current version of Xcode is 26.6.` | Exact event stack remains incompatible. The separate net11 Preview 6 Apple toolchain plus compatible MAUI RC2 package works with Xcode 26.6; no version-check bypass. |
| Preview 6 launch: quoted `mlaunch` executable, `No such file or directory` | Sample-local, SDK/iOS-scoped target removes executable quotes and sets the project run directory. Actual watch launch verified. |
| Preview 6 MAUI: `MAUI1002` experimental SourceGen warning; managed deltas applied but visible XAML stayed unchanged | Selected the iOS-26.5-compatible RC2 MAUI package; label/color/font and preserved state verified on screen. |
| API36 emulator stayed offline; headless run logged `mprotect failed: Permission denied` | Different API30 AVD cold-booted with software rendering. |
| Manually installed fast-deploy APK: `Failed to initialize CoreCLR. Error code: 80070002`, missing `System.Private.CoreLib.dll` | Run target with `EmbedAssembliesIntoApk=true`. |
| MCP: `Another DevFlow session is driving this app (MCP client).` | Closed the previous client gracefully; used one mutation client at a time. |
| VS Code background controls: `no_viable_candidate` | Controls became usable in the maximized Wordzzle window. A native zoom action reported failure despite changing the window; re-observation, not blind retry, established the new state. |
| C# Dev Kit project-system host repeatedly exited 131 | Legacy .NET resolution normalized the Homebrew symlink to the real runtime root; the host stayed running and editor restore succeeded. |
| Full editor restore lacked pinned Android x64 and Mac Catalyst ARM64 runtime packages | Full-project restore with the existing authorized event configuration, then successful editor solution restore. |
| `Debugging canceled: startup project not found.` | Relative/variable project paths failed; an explicit absolute path in the ignored local launch configuration resolved Wordzzle. |
| `save active debug target failed` / `save active platform failed` | Explicit `platform` and `device` plus launch-configuration mode produced a real F5/CoreCLR session. The earlier `skipDebug=true` build-only trial is not counted. |
| Desktop became locked/secure during the active debug session | Automation refused further desktop input. No unlock or foreground workaround attempted; remaining IDE/agent evidence is blocked, not passed. |
| Mobile provider reported the running custom-port emulator as stopped | Explicit ADB target used instead; no device data erased. |
| Mobile provider recording call failed for that custom-port emulator | Recorded the same selected device using Android's `screenrecord`; validated decoded frames and finalized MP4s. |

Searches in `dotnet/macios`, `dotnet/maui`, `dotnet/sdk`, and `dotnet/android`
did not identify matching issues for the searched Xcode, RC2 Hot Reload, or
CoreCLR deployment messages. That is not proof there are no related issues.
These are observed setup/tooling failures, not established product root causes.
Private event logs remain local. Any report must use the appropriate tracker,
one problem per report, with sanitized evidence and the actual launch method.

## Model tests

```bash
dotnet test Tests/Wordzzle.Tests.csproj
```

The net11 test project links the platform-independent game and lab state.
It covers all four word lengths, duplicate counts, invalid/incomplete guesses,
six-attempt loss, valid and invalid persistence, transactional restore,
name validation and binding notifications. These tests do not substitute for
the device, debugger or Hot Reload scenarios above. All 15 tests passed on
the pinned net11 SDK.

A final ordinary Android build also completed with zero warnings/errors and
without the DevFlow package/define. After deployment and cold launch, the
source-backed `Updated live` label remained and the first counter tap changed
0 to 2. The final source changes therefore do not depend on a surviving
in-memory Hot Reload session.

All 15 tests also passed under the iOS Preview 6 SDK profile. Its final ordinary
iOS build completed with zero warnings/errors and no DevFlow package or
compilation symbol. After installation, cold-launched PID 29213 restored the
solved game ([native capture](Screenshots/ios-09-final-cold-launch.png)).
SDK/property checks confirmed the parent Android SDK and MAUI pins were
unchanged, and Xcode remained 26.6.
