#if ANDROID
using System;
using System.Threading.Tasks;
using Google.Android.Material.Button;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using AColor = Android.Graphics.Color;
using AComplexUnitType = Android.Util.ComplexUnitType;
using ATextChangedEventArgs = Android.Text.TextChangedEventArgs;
using ATypedValue = Android.Util.TypedValue;
using AViewTreeObserver = Android.Views.ViewTreeObserver;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue25834")]
	public class Issue25834 : ControlsHandlerTestBase
	{
		const string Caption = "Settings";

		[Fact]
		public async Task DefaultPaddingDoesNotClipTextChangedAfterAttachment()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var defaultPaddingButton = CreateButton();
			var zeroPaddingButton = CreateButton();
			zeroPaddingButton.Padding = new Thickness(0);

			var defaultPaddingGrid = CreateButtonGrid(defaultPaddingButton);
			var zeroPaddingGrid = CreateButtonGrid(zeroPaddingButton);
			var page = new ContentPage
			{
				BackgroundColor = Colors.White,
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = new Thickness(24),
						Spacing = 16,
						Children = { defaultPaddingGrid, zeroPaddingGrid }
					}
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var defaultHandler = Assert.IsType<ButtonHandler>(defaultPaddingButton.Handler);
				var zeroHandler = Assert.IsType<ButtonHandler>(zeroPaddingButton.Handler);
				var defaultNativeButton = Assert.IsAssignableFrom<MaterialButton>(defaultHandler.PlatformView);
				var zeroNativeButton = Assert.IsAssignableFrom<MaterialButton>(zeroHandler.PlatformView);

				Assert.Same(defaultPaddingButton, defaultHandler.VirtualView);
				Assert.Same(zeroPaddingButton, zeroHandler.VirtualView);
				Assert.NotNull(defaultNativeButton.Parent);
				Assert.NotNull(zeroNativeButton.Parent);
				Assert.True(defaultNativeButton.IsAttachedToWindow);
				Assert.True(zeroNativeButton.IsAttachedToWindow);

				var displayMetrics = defaultNativeButton.Resources.DisplayMetrics;
				var density = displayMetrics.Density;
				var expectedWidth = 80 * density;
				var expectedHeight = 48 * density;
				var sizeTolerance = 2 * density;

				AssertWithinTolerance(defaultNativeButton.Width, expectedWidth, sizeTolerance, "default-padding width");
				AssertWithinTolerance(defaultNativeButton.Height, expectedHeight, sizeTolerance, "default-padding height");
				AssertWithinTolerance(zeroNativeButton.Width, expectedWidth, sizeTolerance, "zero-padding width");
				AssertWithinTolerance(zeroNativeButton.Height, expectedHeight, sizeTolerance, "zero-padding height");

				var defaultTextCallbacks = -1;
				var zeroTextCallbacks = -1;
				var defaultLayoutCallbacks = -1;
				var zeroLayoutCallbacks = -1;
				void OnDefaultTextChanged(object sender, ATextChangedEventArgs args) => defaultTextCallbacks++;
				void OnZeroTextChanged(object sender, ATextChangedEventArgs args) => zeroTextCallbacks++;

				var initialDefaultTextLayout = defaultNativeButton.Layout;
				var initialZeroTextLayout = zeroNativeButton.Layout;
				Assert.NotNull(initialDefaultTextLayout);
				Assert.NotNull(initialZeroTextLayout);

				using var defaultLayoutListener = new PreDrawListener { Callback = () => defaultLayoutCallbacks++ };
				using var zeroLayoutListener = new PreDrawListener { Callback = () => zeroLayoutCallbacks++ };
				var defaultViewTreeObserver = defaultNativeButton.ViewTreeObserver;
				var zeroViewTreeObserver = zeroNativeButton.ViewTreeObserver;
				defaultNativeButton.TextChanged += OnDefaultTextChanged;
				zeroNativeButton.TextChanged += OnZeroTextChanged;
				defaultViewTreeObserver.AddOnPreDrawListener(defaultLayoutListener);
				zeroViewTreeObserver.AddOnPreDrawListener(zeroLayoutListener);

				try
				{
					defaultTextCallbacks = 0;
					zeroTextCallbacks = 0;
					defaultLayoutCallbacks = 0;
					zeroLayoutCallbacks = 0;

					defaultPaddingButton.Text = Caption;
					zeroPaddingButton.Text = Caption;

					await AssertEventually(
						() => defaultTextCallbacks > 0 && defaultNativeButton.Text == Caption,
						message: "The default-padding button did not receive its post-attachment native text callback.");
					await AssertEventually(
						() => zeroTextCallbacks > 0 && zeroNativeButton.Text == Caption,
						message: "The zero-padding button did not receive its post-attachment native text callback.");
					await AssertEventually(
						() => defaultLayoutCallbacks > 0 &&
							defaultNativeButton.Layout != null &&
							!ReferenceEquals(initialDefaultTextLayout, defaultNativeButton.Layout),
						message: "The default-padding button did not rebuild its native text layout after the trigger.");
					await AssertEventually(
						() => zeroLayoutCallbacks > 0 &&
							zeroNativeButton.Layout != null &&
							!ReferenceEquals(initialZeroTextLayout, zeroNativeButton.Layout),
						message: "The zero-padding button did not rebuild its native text layout after the trigger.");

					Assert.Same(defaultPaddingButton, defaultHandler.VirtualView);
					Assert.Same(zeroPaddingButton, zeroHandler.VirtualView);
					Assert.Equal(Caption, defaultNativeButton.Text);
					Assert.Equal(Caption, zeroNativeButton.Text);
					Assert.Equal(Colors.Black, new AColor(defaultNativeButton.CurrentTextColor).ToColor());
					Assert.Equal(Colors.Black, new AColor(zeroNativeButton.CurrentTextColor).ToColor());
					Assert.NotNull(defaultNativeButton.Background);
					Assert.NotNull(zeroNativeButton.Background);

					var expectedTextSize = ATypedValue.ApplyDimension(AComplexUnitType.Sp, 20, displayMetrics);
					AssertWithinTolerance(defaultNativeButton.TextSize, expectedTextSize, density, "default-padding text size");
					AssertWithinTolerance(zeroNativeButton.TextSize, expectedTextSize, density, "zero-padding text size");
					AssertWithinTolerance(defaultNativeButton.Width, expectedWidth, sizeTolerance, "post-trigger default-padding width");
					AssertWithinTolerance(defaultNativeButton.Height, expectedHeight, sizeTolerance, "post-trigger default-padding height");
					AssertWithinTolerance(zeroNativeButton.Width, expectedWidth, sizeTolerance, "post-trigger zero-padding width");
					AssertWithinTolerance(zeroNativeButton.Height, expectedHeight, sizeTolerance, "post-trigger zero-padding height");

					Assert.NotNull(defaultNativeButton.Layout);
					Assert.NotNull(zeroNativeButton.Layout);
					Assert.True(defaultNativeButton.Layout.LineCount > 0);
					Assert.True(zeroNativeButton.Layout.LineCount > 0);

					var zeroContentWidth = zeroNativeButton.Width - zeroNativeButton.PaddingLeft - zeroNativeButton.PaddingRight;
					var zeroTextWidth = zeroNativeButton.Paint.MeasureText(zeroNativeButton.Text);
					AssertWithinTolerance(zeroContentWidth, expectedWidth, sizeTolerance, "zero-padding content width");
					Assert.True(
						zeroTextWidth <= zeroContentWidth + sizeTolerance,
						$"The zero-padding oracle clipped '{Caption}': content width={zeroContentWidth}, text width={zeroTextWidth}.");

					var defaultHorizontalPadding = defaultNativeButton.PaddingLeft + defaultNativeButton.PaddingRight;
					var defaultContentWidth = defaultNativeButton.Width - defaultHorizontalPadding;
					var defaultTextWidth = defaultNativeButton.Paint.MeasureText(defaultNativeButton.Text);
					Assert.True(
						Math.Abs(defaultContentWidth - expectedWidth) <= sizeTolerance &&
						defaultTextWidth <= defaultContentWidth + sizeTolerance,
						$"Issue25834 default-padding button clipped '{Caption}': native width={defaultNativeButton.Width}, content width={defaultContentWidth}, text width={defaultTextWidth}, horizontal padding={defaultHorizontalPadding}, expected content width={expectedWidth}.");
				}
				finally
				{
					defaultNativeButton.TextChanged -= OnDefaultTextChanged;
					zeroNativeButton.TextChanged -= OnZeroTextChanged;
					defaultViewTreeObserver.RemoveOnPreDrawListener(defaultLayoutListener);
					zeroViewTreeObserver.RemoveOnPreDrawListener(zeroLayoutListener);
				}
			});
		}

		static NoTabStopButton CreateButton() =>
			new NoTabStopButton
			{
				Text = string.Empty,
				FontSize = 20,
				BackgroundColor = Colors.Transparent,
				TextColor = Colors.Black,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			};

		static Grid CreateButtonGrid(Button button) =>
			new Grid
			{
				WidthRequest = 80,
				HeightRequest = 48,
				HorizontalOptions = LayoutOptions.Start,
				Children = { button }
			};

		static void AssertWithinTolerance(float actual, float expected, float tolerance, string measurement) =>
			Assert.True(
				Math.Abs(actual - expected) <= tolerance,
				$"Unexpected {measurement}: actual={actual}, expected={expected}, tolerance={tolerance}.");

		sealed class PreDrawListener : Java.Lang.Object, AViewTreeObserver.IOnPreDrawListener
		{
			public required Action Callback { get; init; }

			public bool OnPreDraw()
			{
				Callback();
				return true;
			}
		}

		sealed class NoTabStopButton : Button
		{
		}
	}
}
#endif

