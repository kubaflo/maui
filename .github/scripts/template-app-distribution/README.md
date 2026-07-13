# Template App Distribution

Builds a fresh .NET MAUI app from the packaged templates for each platform/variant and
either (a) uploads the results as GitHub artifacts (`publish=false`, a **dry run**) or
(b) signs and publishes them to Google Play / TestFlight (`publish=true`).

The workflow lives in `.github/workflows/template-app-distribution.yml`. Trigger it from the
**Actions** tab with *Run workflow* and pick the source branch (`main`, `net10.0`, `net11.0`
or a `release/*` branch) plus whether to publish.

## What you get, per platform

The goal is that **every artifact a tester downloads can actually be installed** without an
App Store / Play account. The build script therefore emits two things:

- `package_path` — the **store** package (`.aab` / App Store `.ipa` / Mac App Store `.pkg`).
  Consumed only by the Google Play / TestFlight upload steps.
- `sideload_package_path` — the **directly installable** artifact. This is what the dry-run
  job and the publish "artifact copy" step upload for testers.

| Platform | Dry-run artifact (`publish=false`) | Publish store target | Sideloadable artifact on publish |
| --- | --- | --- | --- |
| **Android** | Debug-signed **APK** (installs via `adb install` / file manager) | `.aab` → Google Play | Release-signed **APK** |
| **Windows** | **Self-contained** unpackaged zip (no runtime install needed) | same zip | same zip |
| **iOS** | `.app` zip (Simulator) | App Store `.ipa` → TestFlight | ad-hoc `.ipa` *(only if the ad-hoc secret is set — see below)* |
| **macOS (Mac Catalyst)** | Universal `.app` zip | Mac App Store `.pkg` → TestFlight | notarized `.app` zip *(only if the Developer ID secrets are set — see below)* |

### Why the previous artifacts failed to install

- **Android** — only an `.aab` was produced. An `.aab` can *only* be consumed by Google Play,
  so the ZIP had nothing to sideload. Fixed by also building an installable APK.
- **Windows** — published framework-dependent, so it needed the exact .NET preview desktop
  runtime and still showed the "install .NET" screen. Fixed with `-p:SelfContained=true`.
- **iOS** — the IPA was signed with the App Store / TestFlight profile, which Apple refuses to
  install directly (`0xe800801f "Attempted to install a Beta profile without the proper
  entitlement"`). Fixed by an optional ad-hoc-signed IPA.
- **macOS** — the `.pkg` was Mac App Store signed and defaulted to `maccatalyst-x64` (Rosetta),
  so launching it outside the store gave `SIGKILL (Code Signature Invalid)` /
  `Taskgated Invalid Signature`. Fixed by a universal build plus an optional
  Developer-ID-signed, notarized `.app`.

## Install instructions for testers

- **Android** — download the APK, then `adb install app.apk` (or copy to the device and open
  it; enable "install unknown apps"). The dry-run APK is debug-signed and installs on any
  device/emulator.
- **Windows** — unzip and run the `.exe`. Because the app is self-contained no .NET runtime
  install is required. (SmartScreen may warn for an unsigned app — *More info → Run anyway*.)
- **iOS** — the `.app` zip runs in the iOS **Simulator** (`xcrun simctl install booted App.app`).
  Installing on a physical device requires the ad-hoc IPA (secret-gated, below) and the device
  UDID to be registered in the ad-hoc profile.
- **macOS** — the dry-run `.app` is unsigned; remove the quarantine flag before launching:
  `xattr -dr com.apple.quarantine "MyApp.app"` then open it. A launch-anywhere build for other
  users requires the notarized artifact (secret-gated, below).

## Secrets & variables

The header of `template-app-distribution.yml` is the source of truth. Summary:

**Required for `publish=true`** (protected `template-app-distribution` environment): the Android
keystore, the Google Play service account JSON, the Apple distribution certificate, the App Store
/ Mac App Store provisioning profiles, and the App Store Connect API key. `publish=false` needs
**none** of these — it produces the installable Android APK and self-contained Windows zip
immediately.

**Optional — enable the sideloadable iOS / macOS artifacts:**

- `TEMPLATE_APP_{BLANK,SAMPLE}_IOS_ADHOC_PROVISIONING_PROFILE_BASE64` — ad-hoc distribution
  provisioning profiles (they reuse the existing Apple Distribution certificate). With these set,
  `publish=true` also produces an installable ad-hoc `.ipa`.
- `TEMPLATE_APP_MAC_DEVELOPER_ID_APPLICATION_CERTIFICATE_BASE64` /
  `TEMPLATE_APP_MAC_DEVELOPER_ID_APPLICATION_CERTIFICATE_PASSWORD` — a **Developer ID
  Application** signing certificate (`.p12`).
- `TEMPLATE_APP_{BLANK,SAMPLE}_MACCATALYST_DEVELOPERID_PROVISIONING_PROFILE_BASE64` — Developer ID
  Mac Catalyst provisioning profiles. With these set, `publish=true` also produces a
  Developer-ID-signed, **notarized** `.app` zip that launches on any Mac. Notarization reuses the
  existing `TEMPLATE_APPSTORE_CONNECT_*` API key.

If the optional Apple secrets are absent, the workflow still succeeds and simply falls back to
uploading the store `.ipa` / `.pkg` (which stay TestFlight-only). No secret is ever required for
the Android and Windows fixes.
