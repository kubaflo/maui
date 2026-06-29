using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;

namespace Maui.Controls.Sample;

// Demonstrates the retention caused by subscribing a page/view-model to a STATIC
// Essentials sensor event (Accelerometer.ReadingChanged) and forgetting to unsubscribe.
//
// The static event is backed by a plain delegate on an app-lifetime singleton
// (Accelerometer.Default), so every subscriber that is not removed with `-=` stays
// alive for the life of the process. Calling Accelerometer.Stop() only stops the
// platform monitor; it does NOT clear the managed delegate.
//
// NOTE: the leak is the `+=` subscription itself and is independent of Start()/hardware,
// so it reproduces on emulators and simulators (where the sensor may be unavailable).
static class LeakHarness
{
	public const int DefaultPages = 30;
	public const int DefaultPayloadMB = 3;
	public const string ResultMarker = ">>>SENSOR_LEAK_REPRO>>>";

	public enum Scenario
	{
		Control,    // page never subscribes to the shared sensor event
		Leaky,      // page subscribes, then only Stop() on disappear (forgets -=)
		Mitigation, // page subscribes, then -= on disappear (the fix)
	}

	// Represents a page/view-model that reacts to accelerometer readings and owns a payload.
	sealed class SensorPage
	{
		readonly byte[] _payload;
		EventHandler<AccelerometerChangedEventArgs>? _handler;

		public SensorPage(int payloadBytes)
		{
			_payload = new byte[payloadBytes];
			// Touch each page so the allocation is actually committed.
			for (int i = 0; i < _payload.Length; i += 4096)
				_payload[i] = 0xAB;
		}

		public void OnAppearing_Subscribe()
		{
			_handler = OnReadingChanged;
			Accelerometer.ReadingChanged += _handler;
			try
			{
				if (!Accelerometer.Default.IsMonitoring)
					Accelerometer.Default.Start(SensorSpeed.UI);
			}
			catch
			{
				// Sensor hardware may be unavailable on emulators/simulators; the
				// managed subscription (and therefore the leak) is unaffected.
			}
		}

		// Realistic mistake: stop monitoring but forget to unsubscribe.
		public void OnDisappearing_Leaky()
		{
			try { Accelerometer.Default.Stop(); } catch { }
			// BUG: missing  Accelerometer.ReadingChanged -= _handler;
		}

		// Correct cleanup.
		public void OnDisappearing_Fixed()
		{
			if (_handler is not null)
				Accelerometer.ReadingChanged -= _handler;
			try { Accelerometer.Default.Stop(); } catch { }
		}

		void OnReadingChanged(object? sender, AccelerometerChangedEventArgs e)
		{
			// Genuinely use the payload so the closure must keep the page alive.
			_ = _payload.Length;
		}
	}

	static string RunScenario(Scenario scenario, int pages, long payloadBytes)
	{
		var refs = new List<WeakReference>(pages);

		// Allocate/navigate in a separate method frame so locals don't pin pages.
		AllocatePages(scenario, pages, payloadBytes, refs);

		for (int i = 0; i < 6; i++)
		{
			GC.Collect();
			GC.WaitForPendingFinalizers();
			GC.Collect();
		}

		int alive = 0;
		foreach (var r in refs)
			if (r.IsAlive)
				alive++;

		long allocated = (long)pages * payloadBytes;
		long retained = (long)alive * payloadBytes;
		double pct = allocated == 0 ? 0 : retained * 100.0 / allocated;

		return string.Format(CultureInfo.InvariantCulture,
			"{0,-11}: alive {1,3}/{2} pages, retained {3,7:F1} MB ({4,5:F1}% of {5:F0} MB)",
			scenario, alive, pages, retained / 1024.0 / 1024.0, pct, allocated / 1024.0 / 1024.0);
	}

	static void AllocatePages(Scenario scenario, int pages, long payloadBytes, List<WeakReference> refs)
	{
		for (int i = 0; i < pages; i++)
		{
			var page = new SensorPage((int)payloadBytes);

			switch (scenario)
			{
				case Scenario.Leaky:
					page.OnAppearing_Subscribe();
					page.OnDisappearing_Leaky();
					break;
				case Scenario.Mitigation:
					page.OnAppearing_Subscribe();
					page.OnDisappearing_Fixed();
					break;
				case Scenario.Control:
				default:
					break;
			}

			refs.Add(new WeakReference(page));
			page = null;
		}
	}

	public static string RunAll(int pages = DefaultPages, int payloadMB = DefaultPayloadMB)
	{
		long payloadBytes = (long)payloadMB * 1024 * 1024;

		var sb = new StringBuilder();
		sb.AppendLine(ResultMarker);
		sb.AppendLine(string.Format(CultureInfo.InvariantCulture,
			"SensorSubscriptionLeakRepro  pages={0}  payloadMB={1}", pages, payloadMB));

		// Control/Mitigation leave no subscriptions behind; run Leaky last.
		sb.AppendLine(RunScenario(Scenario.Control, pages, payloadBytes));
		sb.AppendLine(RunScenario(Scenario.Mitigation, pages, payloadBytes));
		sb.AppendLine(RunScenario(Scenario.Leaky, pages, payloadBytes));
		sb.AppendLine(ResultMarker);

		var report = sb.ToString();

		Console.WriteLine(report);
		try
		{
			var path = System.IO.Path.Combine(FileSystem.AppDataDirectory, "sensorleakrepro-results.txt");
			System.IO.File.WriteAllText(path, report);
			Console.WriteLine($"{ResultMarker} results file: {path}");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"{ResultMarker} could not write results file: {ex.Message}");
		}

		return report;
	}
}
