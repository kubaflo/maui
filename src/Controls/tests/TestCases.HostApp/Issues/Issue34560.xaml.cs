#if IOS
using CoreAnimation;
using CoreGraphics;
using Foundation;
using UIKit;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34560, "Switch iOS Liquid glass rendering issue", PlatformAffected.iOS)]
public partial class Issue34560 : ContentPage
{
#if IOS
	const double PixelMismatchTolerance = 0.01;

	CADisplayLink _displayLink;
	byte[] _previousPixels;
	int _stableFrameCount;
	int _measuredFrameCount;
	int _toggleGeneration = -1;
	double _offMismatch = -1;
#endif

	public Issue34560()
	{
		InitializeComponent();

#if IOS
		if (OperatingSystem.IsIOSVersionAtLeast(26))
		{
			MeasurementLabel.Text = "MEASURING OFF";
			Loaded += OnLoaded;
		}
		else
		{
			MeasurementLabel.Text = "UNSUPPORTED: iOS 26 required";
		}
#else
		MeasurementLabel.Text = "UNSUPPORTED: iOS only";
#endif
	}

#if IOS
	void OnLoaded(object sender, EventArgs e)
	{
		Loaded -= OnLoaded;
		StartMeasurement(false);
	}
#endif

	void OnSwitchToggled(object sender, ToggledEventArgs e)
	{
#if IOS
		if (!OperatingSystem.IsIOSVersionAtLeast(26))
			return;

		_toggleGeneration++;
		MeasurementLabel.Text = $"MEASURING ON generation={_toggleGeneration} toggled={e.Value.ToString().ToLowerInvariant()}";
		StartMeasurement(true);
#endif
	}

#if IOS
	void StartMeasurement(bool isOn)
	{
		StopDisplayLink();
		_previousPixels = null;
		_stableFrameCount = 0;
		_measuredFrameCount = 0;

		_displayLink = CADisplayLink.Create(() => MeasureDisplayFrame(isOn));
		_displayLink.AddToRunLoop(NSRunLoop.Current, NSRunLoopMode.Common);
	}

	void MeasureDisplayFrame(bool isOn)
	{
		_measuredFrameCount++;

		if (!TryCaptureMauiSwitch(out var pixels, out var frame, out var windowSize))
		{
			if (_measuredFrameCount >= 120)
			{
				StopDisplayLink();
				MeasurementLabel.Text = "ERROR: Switch did not produce an in-window render frame";
			}

			return;
		}

		if (_previousPixels is not null && pixels.AsSpan().SequenceEqual(_previousPixels))
			_stableFrameCount++;
		else
			_stableFrameCount = 0;

		_previousPixels = pixels;
		if (_stableFrameCount < 2)
		{
			if (_measuredFrameCount >= 120)
			{
				StopDisplayLink();
				MeasurementLabel.Text = "ERROR: Switch rendering did not stabilize";
			}

			return;
		}

		StopDisplayLink();

		if (!TryCaptureNativeReference(frame, out var referencePixels))
		{
			MeasurementLabel.Text = "ERROR: Native UISwitch reference could not be rendered";
			return;
		}

		var mismatch = CalculateMismatch(pixels, referencePixels);
		var pixelCount = pixels.Length / 4;
		var geometry = FormattableString.Invariant(
			$"frameX={frame.X:F1} frameY={frame.Y:F1} frameWidth={frame.Width:F1} frameHeight={frame.Height:F1} pixels={pixelCount} windowWidth={windowSize.Width:F1} windowHeight={windowSize.Height:F1}");

		if (isOn)
		{
			MeasurementLabel.Text = FormattableString.Invariant(
				$"ON off={_offMismatch:F6} on={mismatch:F6} tolerance={PixelMismatchTolerance:F6} {geometry} generation={_toggleGeneration} toggled={IssueSwitch.IsToggled.ToString().ToLowerInvariant()}");
		}
		else
		{
			_offMismatch = mismatch;
			MeasurementLabel.Text = FormattableString.Invariant(
				$"OFF off={mismatch:F6} tolerance={PixelMismatchTolerance:F6} {geometry} generation={_toggleGeneration}");
		}
	}

	bool TryCaptureMauiSwitch(out byte[] pixels, out CGRect frame, out CGSize windowSize)
	{
		pixels = Array.Empty<byte>();
		frame = CGRect.Empty;
		windowSize = CGSize.Empty;

		if (IssueSwitch.Handler?.PlatformView is not UISwitch nativeSwitch || nativeSwitch.Window is not UIWindow window)
			return false;

		frame = nativeSwitch.ConvertRectToView(nativeSwitch.Bounds, window);
		windowSize = window.Bounds.Size;
		return TryCaptureWindowRegion(window, frame, out pixels);
	}

	bool TryCaptureNativeReference(CGRect frame, out byte[] pixels)
	{
		pixels = Array.Empty<byte>();

		if (IssueSwitch.Handler?.PlatformView is not UISwitch nativeSwitch || nativeSwitch.Window is not UIWindow window)
			return false;

		var wasHidden = nativeSwitch.Hidden;
		using var referenceSwitch = new UISwitch(frame)
		{
			On = nativeSwitch.On,
			Enabled = nativeSwitch.Enabled,
			AccessibilityTraits = nativeSwitch.AccessibilityTraits,
			SemanticContentAttribute = nativeSwitch.SemanticContentAttribute
		};

		nativeSwitch.Hidden = true;
		window.AddSubview(referenceSwitch);
		window.LayoutIfNeeded();
		var captured = TryCaptureWindowRegion(window, frame, out pixels);
		referenceSwitch.RemoveFromSuperview();
		nativeSwitch.Hidden = wasHidden;
		window.LayoutIfNeeded();
		return captured;
	}

	static bool TryCaptureWindowRegion(UIWindow window, CGRect frame, out byte[] pixels)
	{
		pixels = Array.Empty<byte>();
		if (frame.Width <= 0 || frame.Height <= 0)
			return false;

		using var renderer = new UIGraphicsImageRenderer(window.Bounds.Size, new UIGraphicsImageRendererFormat
		{
			Opaque = false,
			Scale = window.Screen.Scale
		});
		using var image = renderer.CreateImage(_ => window.DrawViewHierarchy(window.Bounds, true));
		var cgImage = image.CGImage;
		var scale = window.Screen.Scale;
		var left = (int)Math.Floor(frame.Left * scale);
		var top = (int)Math.Floor(frame.Top * scale);
		var right = (int)Math.Ceiling(frame.Right * scale);
		var bottom = (int)Math.Ceiling(frame.Bottom * scale);

		if (left < 0 || top < 0 || right > cgImage.Width || bottom > cgImage.Height || right <= left || bottom <= top)
			return false;

		using var data = cgImage.DataProvider.CopyData();
		var source = data.ToArray();
		var bytesPerPixel = (int)cgImage.BitsPerPixel / 8;
		var bytesPerRow = (int)cgImage.BytesPerRow;
		pixels = new byte[(right - left) * (bottom - top) * 4];
		var destination = 0;

		for (var y = top; y < bottom; y++)
		{
			for (var x = left; x < right; x++)
			{
				var sourceOffset = y * bytesPerRow + x * bytesPerPixel;
				pixels[destination++] = source[sourceOffset];
				pixels[destination++] = source[sourceOffset + 1];
				pixels[destination++] = source[sourceOffset + 2];
				pixels[destination++] = source[sourceOffset + 3];
			}
		}

		return true;
	}

	static double CalculateMismatch(byte[] actual, byte[] expected)
	{
		if (actual.Length != expected.Length || actual.Length == 0)
			return 1;

		var mismatchedPixels = 0;
		for (var index = 0; index < actual.Length; index += 4)
		{
			if (Math.Abs(actual[index] - expected[index]) > 8 ||
				Math.Abs(actual[index + 1] - expected[index + 1]) > 8 ||
				Math.Abs(actual[index + 2] - expected[index + 2]) > 8 ||
				Math.Abs(actual[index + 3] - expected[index + 3]) > 8)
			{
				mismatchedPixels++;
			}
		}

		return (double)mismatchedPixels / (actual.Length / 4);
	}

	void StopDisplayLink()
	{
		if (_displayLink is null)
			return;

		_displayLink.RemoveFromRunLoop(NSRunLoop.Current, NSRunLoopMode.Common);
		_displayLink.Dispose();
		_displayLink = null;
	}

	protected override void OnDisappearing()
	{
		StopDisplayLink();
		base.OnDisappearing();
	}
#endif
}
