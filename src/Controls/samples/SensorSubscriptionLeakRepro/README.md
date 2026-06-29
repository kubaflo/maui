# SensorSubscriptionLeakRepro

Reproduces a memory leak caused by subscribing a page / view-model to the **static
`Accelerometer.ReadingChanged`** Essentials event and forgetting to unsubscribe.

`Microsoft.Maui.Devices.Sensors.Accelerometer.ReadingChanged` (like `Gyroscope`,
`Magnetometer`, `Compass`, `OrientationSensor`, `Barometer`, `Connectivity`, and
`Battery`) is backed by a **plain multicast delegate on an app-lifetime singleton**
(`Accelerometer.Default`). Every subscriber that is not removed with `-=` is therefore
retained for the entire life of the process. Calling `Accelerometer.Stop()` only stops
the platform sensor; it does **not** clear the managed delegate.

By contrast, `Application.RequestedThemeChanged` uses a `WeakEventManager`, so forgetting
to unsubscribe there does *not* leak. The Essentials static events do not, which is the
inconsistency this repro highlights.

> The leak is the `+=` subscription itself, independent of `Start()`/hardware, so it
> reproduces on emulators and simulators where the sensor may be unavailable.

## Scenarios

The app allocates `pages` (default 30) objects, each owning a `payloadMB` (default 3 MB)
byte array, and measures how many survive a full GC after "navigating away":

1. **Control** – the page never subscribes to the shared sensor event.
2. **Leaky** – the page subscribes in `OnAppearing` and only calls `Accelerometer.Stop()`
   in `OnDisappearing` (the realistic mistake — it forgets `-=`).
3. **Mitigation** – the page subscribes, then calls `-=` in `OnDisappearing` (the fix).

## Build

```bash
# from the repo root, build the build tasks first
dotnet build ./Microsoft.Maui.BuildTasks.slnf

dotnet build src/Controls/samples/SensorSubscriptionLeakRepro/SensorSubscriptionLeakRepro.csproj -f net10.0-maccatalyst
dotnet build src/Controls/samples/SensorSubscriptionLeakRepro/SensorSubscriptionLeakRepro.csproj -f net10.0-android
dotnet build src/Controls/samples/SensorSubscriptionLeakRepro/SensorSubscriptionLeakRepro.csproj -f net10.0-ios -p:RuntimeIdentifier=iossimulator-arm64
```

## Run

The harness **auto-runs on launch** and prints results (and writes
`sensorleakrepro-results.txt` to the app data dir). Results are bracketed by the
`>>>SENSOR_LEAK_REPRO>>>` marker.

```bash
# Mac Catalyst
dotnet build src/Controls/samples/SensorSubscriptionLeakRepro/SensorSubscriptionLeakRepro.csproj -t:Run -f net10.0-maccatalyst

# Android (then read logcat)
dotnet build src/Controls/samples/SensorSubscriptionLeakRepro/SensorSubscriptionLeakRepro.csproj -t:Run -f net10.0-android
adb logcat -d | grep SENSOR_LEAK_REPRO
adb shell run-as com.microsoft.maui.sensorleakrepro cat files/sensorleakrepro-results.txt
```

## Observed results (defaults: 30 pages × 3 MB = 90 MB)

| Target | Control | Mitigation | Leaky (retained) |
|--------|---------|------------|------------------|
| Mac Catalyst | 0/30 pages, 0.0 MB | 0/30 pages, 0.0 MB | **30/30 pages, 90.0 MB (100%)** |
| Android (API 30 emulator) | 0/30 pages, 0.0 MB | 0/30 pages, 0.0 MB | **30/30 pages, 90.0 MB (100%)** |
| iOS 18.x simulator | 0/30 pages, 0.0 MB | 0/30 pages, 0.0 MB | **30/30 pages, 90.0 MB (100%)** |

On every target the `Leaky` run retains the full payload after a forced GC, while both
`Control` and `Mitigation` release everything — proving the unremoved subscription to the
static `Accelerometer.ReadingChanged` is the retention root.
