#if ANDROID
using System;
using System.Threading.Tasks;
using AndroidX.AppCompat.Widget;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category("Issue25834")]
	public class Issue25834 : ControlsHandlerTestBase
	{
		const string DisplayText = "Settings";
		const string MeasurementText = "Settings Settings Settings";

		[Fact]
		public async Task DefaultPaddingDoesNotClipTextAtMeasuredWidth()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
					handlers.AddHandler<IContentView, ContentViewHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var affectedButton = new NoTabStopButton
			{
				Text = string.Empty,
				BackgroundColor = Colors.Transparent,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
			};
			var referenceButton = new NoTabStopButton
			{
				Text = string.Empty,
				BackgroundColor = Colors.Transparent,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill,
				Padding = 0,
			};
			var affectedHost = new CustomImageButton
			{
				Content = affectedButton,
				HorizontalOptions = LayoutOptions.Start,
			};
			var referenceHost = new CustomImageButton
			{
				Content = referenceButton,
				HorizontalOptions = LayoutOptions.Start,
			};
			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 12,
				Children =
				{
					new Label { Text = "Issue 25834: Android Button default padding" },
					new Label { Text = "Affected: default Padding" },
					affectedHost,
					new Label { Text = "Reference: Padding=0" },
					referenceHost,
				},
			};
			var page = new ContentPage
			{
				Content = new ScrollView { Content = stack },
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				affectedButton.Text = MeasurementText;
				referenceButton.Text = MeasurementText;

				await AssertEventually(
					() => affectedButton.Width > 0 && referenceButton.Width > 0,
					message: "The measurement buttons did not complete their initial layout.");

				var affectedMeasurement = affectedButton.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
				var referenceMeasurement = referenceButton.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;

				affectedButton.Text = DisplayText;
				referenceButton.Text = DisplayText;
				var constrainedWidth = referenceButton.Measure(double.PositiveInfinity, double.PositiveInfinity).Width;
				double observedWidth = -1;
				affectedHost.SizeChanged += (_, _) =>
				{
					if (Math.Abs(affectedHost.Width - constrainedWidth) < 0.01)
						observedWidth = affectedHost.Width;
				};

				affectedHost.WidthRequest = constrainedWidth;
				referenceHost.WidthRequest = constrainedWidth;

				await AssertEventually(
					() => observedWidth >= 0,
					message: "The affected host did not report the post-trigger constrained layout.");
				Assert.True(observedWidth >= 0, "The post-trigger SizeChanged callback was not observed.");

				var affectedHandler = Assert.IsType<ButtonHandler>(affectedButton.Handler);
				var referenceHandler = Assert.IsType<ButtonHandler>(referenceButton.Handler);
				Assert.Same(affectedButton, affectedHandler.VirtualView);
				Assert.Same(referenceButton, referenceHandler.VirtualView);
				Assert.Same(affectedButton, affectedHost.Content);
				Assert.Same(referenceButton, referenceHost.Content);

				var affectedNative = Assert.IsAssignableFrom<AppCompatButton>(affectedHandler.PlatformView);
				var referenceNative = Assert.IsAssignableFrom<AppCompatButton>(referenceHandler.PlatformView);
				var density = affectedNative.Resources.DisplayMetrics.Density;
				var scaledDensity = affectedNative.Resources.DisplayMetrics.ScaledDensity;
				var fontScale = affectedNative.Resources.Configuration.FontScale;
				var layoutDirection = affectedNative.LayoutDirection;
				var expectedWidthPixels = constrainedWidth * density;
				const double widthTolerancePixels = 1.5;

				await AssertEventually(
					() => Math.Abs(affectedNative.Width - expectedWidthPixels) <= widthTolerancePixels &&
						Math.Abs(referenceNative.Width - expectedWidthPixels) <= widthTolerancePixels,
					message: $"Native buttons did not reach the arranged host width {expectedWidthPixels}px.");

				Assert.Equal(DisplayText, affectedNative.Text);
				Assert.Equal(DisplayText, referenceNative.Text);
				Assert.True(Math.Abs(affectedNative.Width - expectedWidthPixels) <= widthTolerancePixels);
				Assert.True(Math.Abs(referenceNative.Width - expectedWidthPixels) <= widthTolerancePixels);

				var referenceTextWidth = referenceNative.Paint.MeasureText(DisplayText);
				var referenceContentWidth = referenceNative.Width - referenceNative.CompoundPaddingLeft - referenceNative.CompoundPaddingRight;
				Assert.True(referenceTextWidth <= referenceContentWidth + 1,
					$"The zero-padding reference must fit: textWidth={referenceTextWidth}, contentWidth={referenceContentWidth}.");

				var affectedTextWidth = affectedNative.Paint.MeasureText(DisplayText);
				var affectedContentWidth = affectedNative.Width - affectedNative.CompoundPaddingLeft - affectedNative.CompoundPaddingRight;
				Assert.True(affectedTextWidth <= affectedContentWidth + 1,
					$"Issue25834 default-padding Button clips Settings text: textWidth={affectedTextWidth}, contentWidth={affectedContentWidth}, " +
					$"nativeWidth={affectedNative.Width}, compoundPadding={affectedNative.CompoundPaddingLeft + affectedNative.CompoundPaddingRight}, " +
					$"density={density}, scaledDensity={scaledDensity}, fontScale={fontScale}, layoutDirection={layoutDirection}, " +
					$"expectedFitWidth={expectedWidthPixels}, defaultMeasurement={affectedMeasurement}, zeroPaddingMeasurement={referenceMeasurement}.");
			});
		}

		sealed class CustomImageButton : ContentView
		{
		}

		sealed class NoTabStopButton : Button
		{
		}
	}
}
#endif

