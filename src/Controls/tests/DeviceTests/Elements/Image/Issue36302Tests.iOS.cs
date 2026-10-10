using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

#if IOS && !MACCATALYST
namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Image)]
	[Category("Issue36302")]
	public class Issue36302 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearingBackgroundColorRestoresTransparentNativeBackground()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Microsoft.Maui.Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<IScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<ImageButton, ImageButtonHandler>();
				});
			});

			var defaultImageButton = new ImageButton();
			await CreateHandlerAndAddToWindow(defaultImageButton, () =>
			{
				AssertAttached(defaultImageButton);
				var nativeDefaultImageButton = Assert.IsType<UIButton>(defaultImageButton.Handler.PlatformView);
				var defaultRgba = GetRgba(nativeDefaultImageButton.BackgroundColor);
				Assert.True(defaultRgba.Alpha <= 0.01,
					$"Default ImageButton native background should be transparent; observed RGBA={Format(defaultRgba)}");
			});

			var headingLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 22,
				Text = "ImageButton BackgroundColor reset"
			};
			var descriptionLabel = new Label
			{
				Text = "The ImageButton should stop showing red after its BackgroundColor is cleared."
			};
			var affectedImageButton = new ImageButton
			{
				BackgroundColor = Colors.CornflowerBlue,
				HeightRequest = 180,
				HorizontalOptions = LayoutOptions.Center,
				Source = "dotnet_bot.png",
				WidthRequest = 180
			};
			var setRedButton = new Button { Text = "Set background to Red" };
			var clearBackgroundButton = new Button { Text = "Clear BackgroundColor" };
			var checkBackgroundButton = new Button { Text = "Check cleared background" };
			var statusLabel = new Label
			{
				FontAttributes = FontAttributes.Bold,
				FontSize = 18,
				Text = "Waiting to apply red"
			};

			var observedStage = -1;
			(double Red, double Green, double Blue, double Alpha) sampledRgba = (-1, -1, -1, -1);

			setRedButton.Clicked += (_, _) =>
			{
				affectedImageButton.BackgroundColor = Colors.Red;
				statusLabel.Text = "Red background ready";
				observedStage = 1;
			};
			clearBackgroundButton.Clicked += (_, _) =>
			{
				affectedImageButton.BackgroundColor = null;
				statusLabel.Text = "BackgroundColor cleared";
				observedStage = 2;
			};
			checkBackgroundButton.Clicked += (_, _) =>
			{
				var nativeImageButton = Assert.IsType<UIButton>(affectedImageButton.Handler.PlatformView);
				sampledRgba = GetRgba(nativeImageButton.BackgroundColor);
				statusLabel.Text = "Native background inspected";
				observedStage = 3;
			};

			var stack = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16
			};
			stack.Add(headingLabel);
			stack.Add(descriptionLabel);
			stack.Add(affectedImageButton);
			stack.Add(setRedButton);
			stack.Add(clearBackgroundButton);
			stack.Add(checkBackgroundButton);
			stack.Add(statusLabel);

			var scrollView = new ScrollView { Content = stack };
			var page = new ContentPage { Content = scrollView };

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				AssertAttached(page);
				AssertAttached(scrollView);
				AssertAttached(stack);
				AssertAttached(headingLabel);
				AssertAttached(descriptionLabel);
				AssertAttached(affectedImageButton);
				AssertAttached(setRedButton);
				AssertAttached(clearBackgroundButton);
				AssertAttached(checkBackgroundButton);
				AssertAttached(statusLabel);

				var nativeImageButton = Assert.IsType<UIButton>(affectedImageButton.Handler.PlatformView);
				var nativeSetRedButton = Assert.IsType<UIButton>(setRedButton.Handler.PlatformView);
				var nativeClearBackgroundButton = Assert.IsType<UIButton>(clearBackgroundButton.Handler.PlatformView);
				var nativeCheckBackgroundButton = Assert.IsType<UIButton>(checkBackgroundButton.Handler.PlatformView);

				await AssertEventually(
					() => RgbaMatches(GetRgba(nativeImageButton.BackgroundColor), GetRgba(Colors.CornflowerBlue.ToPlatform())),
					message: "ImageButton native background did not become CornflowerBlue after attachment");

				nativeSetRedButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await AssertEventually(() => observedStage == 1, message: "Set-red Button.Clicked callback did not run");
				Assert.Equal(Colors.Red, affectedImageButton.BackgroundColor);
				await AssertEventually(
					() => RgbaMatches(GetRgba(nativeImageButton.BackgroundColor), GetRgba(Colors.Red.ToPlatform())),
					message: "ImageButton native background did not become red");

				nativeClearBackgroundButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await AssertEventually(() => observedStage == 2, message: "Clear Button.Clicked callback did not run");
				Assert.Null(affectedImageButton.BackgroundColor);

				nativeCheckBackgroundButton.SendActionForControlEvents(UIControlEvent.TouchUpInside);
				await AssertEventually(() => observedStage == 3, message: "Check Button.Clicked callback did not run");
				Assert.Equal("Native background inspected", statusLabel.Text);
				Assert.Null(affectedImageButton.BackgroundColor);
				Assert.True(sampledRgba.Alpha <= 0.01,
					$"ImageButton native background should be transparent after BackgroundColor is null; observed RGBA={Format(sampledRgba)}");
			});
		}

		static void AssertAttached(VisualElement element)
		{
			Assert.NotNull(element.Handler);
			Assert.NotNull(element.Handler.PlatformView);
		}

		static (double Red, double Green, double Blue, double Alpha) GetRgba(UIColor color)
		{
			if (color is null)
				return (0, 0, 0, 0);

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return ((double)red, (double)green, (double)blue, (double)alpha);
		}

		static bool RgbaMatches(
			(double Red, double Green, double Blue, double Alpha) actual,
			(double Red, double Green, double Blue, double Alpha) expected) =>
			Math.Abs(actual.Red - expected.Red) <= 0.01
			&& Math.Abs(actual.Green - expected.Green) <= 0.01
			&& Math.Abs(actual.Blue - expected.Blue) <= 0.01
			&& Math.Abs(actual.Alpha - expected.Alpha) <= 0.01;

		static string Format((double Red, double Green, double Blue, double Alpha) rgba) =>
			$"({rgba.Red:F3}, {rgba.Green:F3}, {rgba.Blue:F3}, {rgba.Alpha:F3})";
	}
}
#endif

