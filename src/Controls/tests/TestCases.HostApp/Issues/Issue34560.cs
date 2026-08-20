#if IOS
using CoreGraphics;
using Microsoft.Maui;
using UIKit;
#endif

namespace Maui.Controls.Sample.Issues;

[Issue(IssueTracker.Github, 34560, "Switch iOS Liquid glass rendering issue", PlatformAffected.iOS)]
public class Issue34560 : ContentPage
{
	readonly Switch _affectedSwitch;
	readonly Label _resultLabel;

#if IOS
	UISwitch _nativeSwitch;
	UISwitch _nativeReference;
	UIView _renderedSwitch;
	CGRect _renderedCrop;
	int _allowedDifferentPixels;
	int _toggledObservation = -1;
	bool _nativeOnObservation;
	bool _preparing;
	bool _prepared;
#endif

	public Issue34560()
	{
		if (Application.Current is not null)
		{
			Application.Current.UserAppTheme = AppTheme.Light;
		}

		_affectedSwitch = new Switch
		{
			AutomationId = "AffectedSwitch",
			HorizontalOptions = LayoutOptions.Center
		};
		_affectedSwitch.Toggled += OnAffectedSwitchToggled;

		_resultLabel = new Label
		{
			AutomationId = "ResultLabel",
			Text = "Preparing native rendering oracle",
			HorizontalOptions = LayoutOptions.Center
		};

		Content = new VerticalStackLayout
		{
			Padding = 24,
			Spacing = 24,
			VerticalOptions = LayoutOptions.Center,
			Children =
			{
				new Label
				{
					Text = "Default iOS Switch rendering",
					FontSize = 20,
					HorizontalOptions = LayoutOptions.Center
				},
				_affectedSwitch,
				_resultLabel
			}
		};
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

#if IOS
		SizeChanged += OnPageSizeChanged;
		TryPrepareRenderingOracle();
#endif
	}

	protected override void OnDisappearing()
	{
#if IOS
		SizeChanged -= OnPageSizeChanged;
		_nativeReference?.RemoveFromSuperview();
		_nativeReference?.Dispose();
		_nativeReference = null;
#endif

		base.OnDisappearing();
	}

	void OnAffectedSwitchToggled(object sender, ToggledEventArgs e)
	{
#if IOS
		if (!_prepared || !e.Value)
		{
			return;
		}

		_toggledObservation = e.Value ? 1 : 0;
		_nativeOnObservation = _nativeSwitch.On;
		Dispatcher.Dispatch(CaptureOnRendering);
#endif
	}

#if IOS
	void OnPageSizeChanged(object sender, EventArgs e)
	{
		TryPrepareRenderingOracle();
	}

	void TryPrepareRenderingOracle()
	{
		if (_prepared || _preparing)
		{
			return;
		}

		if (!OperatingSystem.IsIOSVersionAtLeast(26))
		{
			_resultLabel.Text = "Unsupported iOS version below 26";
			return;
		}

		if (Width <= 0 || Height <= 0 || Width >= Height ||
			_affectedSwitch.Handler is not IViewHandler handler ||
			handler.PlatformView is not UISwitch nativeSwitch ||
			nativeSwitch.Window is null ||
			nativeSwitch.Bounds.Width <= 0 || nativeSwitch.Bounds.Height <= 0)
		{
			return;
		}

		_preparing = true;
		Dispatcher.Dispatch(() => PrepareRenderingOracle(handler, nativeSwitch));
	}

	void PrepareRenderingOracle(IViewHandler handler, UISwitch nativeSwitch)
	{
		_nativeSwitch = nativeSwitch;
		_renderedSwitch = handler.ContainerView as UIView ?? nativeSwitch;
		var switchCrop = ReferenceEquals(_renderedSwitch, nativeSwitch)
			? nativeSwitch.Bounds
			: nativeSwitch.Frame;
		_renderedCrop = AddCaptureMargin(switchCrop);

		_nativeReference = new UISwitch(CGRect.Empty)
		{
			Bounds = nativeSwitch.Bounds,
			Center = new CGPoint(-200, -200)
		};
		nativeSwitch.Window.AddSubview(_nativeReference);
		_nativeReference.LayoutIfNeeded();

		var style = nativeSwitch.TraitCollection.UserInterfaceStyle;
		var referenceStyle = _nativeReference.TraitCollection.UserInterfaceStyle;
		var scale = nativeSwitch.TraitCollection.DisplayScale;
		var referenceScale = _nativeReference.TraitCollection.DisplayScale;

		if (style != UIUserInterfaceStyle.Light || referenceStyle != style ||
			Math.Abs((double)scale - (double)referenceScale) > 0.01)
		{
			_resultLabel.Text = $"Setup failure: style={style}, referenceStyle={referenceStyle}, scale={scale:0.##}, referenceScale={referenceScale:0.##}";
			return;
		}

		var referenceCrop = AddCaptureMargin(_nativeReference.Bounds);
		var firstReference = Capture(_nativeReference, referenceCrop);
		var secondReference = Capture(_nativeReference, referenceCrop);
		var mauiOff = Capture(_renderedSwitch, _renderedCrop);
		EnsureMatchingDimensions(firstReference, secondReference, mauiOff);

		var referenceSurface = CountSurfacePixels(firstReference);
		var mauiSurface = CountSurfacePixels(mauiOff);
		if (referenceSurface < 100 || mauiSurface < 100)
		{
			_resultLabel.Text = $"Setup failure: switch surface was empty; native={referenceSurface}, maui={mauiSurface}";
			return;
		}

		var stableDifference = CountDifferentPixels(firstReference, secondReference);
		var offDifference = CountDifferentPixels(firstReference, mauiOff);
		const int cleanStateTolerance = 12;
		if (stableDifference > cleanStateTolerance || offDifference > cleanStateTolerance)
		{
			_resultLabel.Text =
				$"Setup failure: clean off-state oracle exceeded tolerance; stability={stableDifference}, offDiff={offDifference}, tolerance={cleanStateTolerance}";
			return;
		}

		_allowedDifferentPixels = Math.Max(stableDifference, offDifference) + 4;
		_prepared = true;
		_preparing = false;
		_resultLabel.Text =
			$"Ready: offDiff={offDifference}, allowance={_allowedDifferentPixels}, dimensions={mauiOff.Width}x{mauiOff.Height}, scale={mauiOff.Scale:0.##}, style={style}, portrait={Width < Height}";
	}

	void CaptureOnRendering()
	{
		if (_toggledObservation != 1 || !_affectedSwitch.IsToggled || !_nativeOnObservation || !_nativeSwitch.On)
		{
			_resultLabel.Text =
				$"Transition failure: managed={_toggledObservation}, isToggled={_affectedSwitch.IsToggled}, nativeOn={_nativeOnObservation}/{_nativeSwitch.On}";
			return;
		}

		_nativeReference.SetState(true, false);
		_nativeReference.LayoutIfNeeded();

		var mauiOn = Capture(_renderedSwitch, _renderedCrop);
		var nativeOn = Capture(_nativeReference, AddCaptureMargin(_nativeReference.Bounds));
		EnsureMatchingDimensions(mauiOn, nativeOn);

		var mauiSurface = CountSurfacePixels(mauiOn);
		var nativeSurface = CountSurfacePixels(nativeOn);
		if (mauiSurface < 100 || nativeSurface < 100)
		{
			_resultLabel.Text = $"Capture failure: managed=1, nativeOn=True, surfaces={mauiSurface}/{nativeSurface}";
			return;
		}

		var differentPixels = CountDifferentPixels(mauiOn, nativeOn);
		var details =
			$"managed=1, nativeOn=True, differing pixels={differentPixels}, allowed pixels={_allowedDifferentPixels}, dimensions={mauiOn.Width}x{mauiOn.Height}, scale={mauiOn.Scale:0.##}";
		_resultLabel.Text = differentPixels <= _allowedDifferentPixels
			? $"PASS: {details}"
			: $"On-state render mismatch: {details}";
	}

	static PixelImage Capture(UIView view, CGRect crop)
	{
		var scale = view.Window?.Screen?.Scale ?? UIScreen.MainScreen.Scale;
		var format = new UIGraphicsImageRendererFormat
		{
			Opaque = false,
			Scale = scale
		};

		using var renderer = new UIGraphicsImageRenderer(crop.Size, format);
		using var image = renderer.CreateImage(context =>
		{
			context.CGContext.TranslateCTM(-crop.X, -crop.Y);
			view.Layer.RenderInContext(context.CGContext);
		});

		var cgImage = image.CGImage;
		var width = (int)cgImage.Width;
		var height = (int)cgImage.Height;
		var pixels = new byte[width * height * 4];
		using var colorSpace = CGColorSpace.CreateDeviceRGB();
		using var bitmapContext = new CGBitmapContext(
			pixels,
			width,
			height,
			8,
			width * 4,
			colorSpace,
			CGBitmapFlags.ByteOrder32Big | CGBitmapFlags.PremultipliedLast);
		bitmapContext.DrawImage(new CGRect(0, 0, width, height), cgImage);

		return new PixelImage(pixels, width, height, (double)scale);
	}

	static CGRect AddCaptureMargin(CGRect bounds)
	{
		const double margin = 8;
		return new CGRect(
			bounds.X - margin,
			bounds.Y - margin,
			bounds.Width + margin * 2,
			bounds.Height + margin * 2);
	}

	static void EnsureMatchingDimensions(params PixelImage[] images)
	{
		var expected = images[0];
		foreach (var image in images)
		{
			if (image.Width != expected.Width || image.Height != expected.Height ||
				image.Pixels.Length != expected.Pixels.Length)
			{
				throw new InvalidOperationException(
					$"Rendering oracle dimensions differed: expected {expected.Width}x{expected.Height}, actual {image.Width}x{image.Height}.");
			}
		}
	}

	static int CountSurfacePixels(PixelImage image)
	{
		var count = 0;
		for (var index = 3; index < image.Pixels.Length; index += 4)
		{
			if (image.Pixels[index] > 8)
			{
				count++;
			}
		}

		return count;
	}

	static int CountDifferentPixels(PixelImage first, PixelImage second)
	{
		EnsureMatchingDimensions(first, second);
		var differentPixels = 0;

		for (var index = 0; index < first.Pixels.Length; index += 4)
		{
			if (Math.Abs(first.Pixels[index] - second.Pixels[index]) > 4 ||
				Math.Abs(first.Pixels[index + 1] - second.Pixels[index + 1]) > 4 ||
				Math.Abs(first.Pixels[index + 2] - second.Pixels[index + 2]) > 4 ||
				Math.Abs(first.Pixels[index + 3] - second.Pixels[index + 3]) > 4)
			{
				differentPixels++;
			}
		}

		return differentPixels;
	}

	readonly record struct PixelImage(byte[] Pixels, int Width, int Height, double Scale);
#endif
}
