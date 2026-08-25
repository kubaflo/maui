#if IOS && !MACCATALYST
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Image)]
	[Category("Issue36302")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36302 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearingImageBackgroundColorRestoresTransparentNativeBackground()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Image, ImageHandler>();
				});
			});

			double transparentBaseline = double.NaN;
			var cleanImage = new Image();

			await CreateHandlerAndAddToWindow(cleanImage, () =>
			{
				var cleanNativeImage = Assert.IsAssignableFrom<UIImageView>(cleanImage.Handler.PlatformView);
				transparentBaseline = GetBackgroundAlpha(cleanNativeImage);
				Assert.InRange(transparentBaseline, 0, 0.01);
			});

			Assert.False(double.IsNaN(transparentBaseline));

			var testImage = new Image
			{
				BackgroundColor = Colors.Blue,
				HeightRequest = 220,
				Source = "dotnet_bot.png",
				WidthRequest = 220,
			};
			var stateLabel = new Label { Text = "Initial state: Image background is blue" };
			var setRedButton = new Button { Text = "Set Image Background Red" };
			var clearBackgroundButton = new Button { Text = "Clear Image Background" };

			setRedButton.Clicked += (_, _) =>
			{
				testImage.BackgroundColor = Colors.Red;
				stateLabel.Text = "Reference state: Image background is red";
			};
			clearBackgroundButton.Clicked += (_, _) =>
			{
				testImage.BackgroundColor = null;
				stateLabel.Text = "After clear: pale green should replace the red background";
			};

			var content = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 20,
						Text = "Image BackgroundColor null transition",
					},
					new Label
					{
						Text = "The pale green page should show through the Image after its red background is cleared.",
					},
					testImage,
					stateLabel,
					setRedButton,
					clearBackgroundButton,
				},
			};
			var page = new ContentPage
			{
				BackgroundColor = Color.FromArgb("#E8F5E9"),
				Content = new ScrollView { Content = content },
				Title = "Image BackgroundColor",
			};

			int backgroundChangeCount = 0;
			object observedBackground = new object();
			testImage.PropertyChanged += OnImagePropertyChanged;

			await CreateHandlerAndAddToWindow<PageHandler>(page, async _ =>
			{
				Assert.NotNull(testImage.Source);
				Assert.Equal(220, testImage.WidthRequest);
				Assert.Equal(220, testImage.HeightRequest);
				Assert.Equal(Colors.Blue, testImage.BackgroundColor);

				var nativeImage = Assert.IsAssignableFrom<UIImageView>(testImage.Handler.PlatformView);
				await AssertEventually(
					() => Math.Abs(nativeImage.Frame.Width - 220) <= 0.01 &&
						Math.Abs(nativeImage.Frame.Height - 220) <= 0.01,
					message: $"Issue36302 Image was not arranged at 220x220: {nativeImage.Frame}.");
				Assert.True(IsRedOrBlue(nativeImage.BackgroundColor, expectRed: false),
					"Issue36302 Image native background was not initially blue.");

				var redButtonHandler = Assert.IsType<ButtonHandler>(setRedButton.Handler);
				redButtonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				Assert.Equal(1, backgroundChangeCount);
				Assert.Equal(Colors.Red, observedBackground);
				Assert.Equal(Colors.Red, testImage.BackgroundColor);
				await AssertEventually(
					() => IsRedOrBlue(nativeImage.BackgroundColor, expectRed: true),
					message: "Issue36302 Image native background did not become red.");

				backgroundChangeCount = 0;
				observedBackground = new object();
				var clearButtonHandler = Assert.IsType<ButtonHandler>(clearBackgroundButton.Handler);
				clearButtonHandler.PlatformView.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				Assert.Equal(1, backgroundChangeCount);
				Assert.Null(observedBackground);
				Assert.Null(testImage.BackgroundColor);

				await Wait(
					() => Math.Abs(GetBackgroundAlpha(nativeImage) - transparentBaseline) <= 0.01);
				double observedAlpha = GetBackgroundAlpha(nativeImage);
				Assert.True(
					Math.Abs(observedAlpha - transparentBaseline) <= 0.01,
					$"Issue36302 Image native background remained opaque after BackgroundColor was cleared: observed alpha {observedAlpha}, expected {transparentBaseline}.");
			});

			testImage.PropertyChanged -= OnImagePropertyChanged;

			void OnImagePropertyChanged(object sender, PropertyChangedEventArgs args)
			{
				if (args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				{
					backgroundChangeCount++;
					observedBackground = testImage.BackgroundColor;
				}
			}
		}

		static double GetBackgroundAlpha(UIImageView imageView) =>
			imageView.BackgroundColor?.CGColor.Alpha ?? 0;

		static bool IsRedOrBlue(UIColor color, bool expectRed)
		{
			if (color is null)
				return false;

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			var expectedRed = expectRed ? 1 : 0;
			var expectedBlue = expectRed ? 0 : 1;
			return Math.Abs(red - expectedRed) <= 0.01 &&
				Math.Abs(green) <= 0.01 &&
				Math.Abs(blue - expectedBlue) <= 0.01 &&
				Math.Abs(alpha - 1) <= 0.01;
		}
	}
}
#endif

