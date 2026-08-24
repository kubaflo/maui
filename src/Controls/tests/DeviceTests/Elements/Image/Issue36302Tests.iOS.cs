#if IOS && !MACCATALYST
using System;
using System.ComponentModel;
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

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Image)]
	[Category("Issue36302")]
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36302 : ControlsHandlerTestBase
	{
		const double ColorTolerance = 0.01;

		[Fact]
		public async Task ClearingBackgroundColorRestoresNativeTransparentBackground()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<ImageButton, ImageButtonHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			double expectedImageAlpha = -1;
			double expectedImageButtonAlpha = -1;
			var calibrationImage = new Image();
			var calibrationImageButton = new ImageButton();
			var calibrationLayout = new VerticalStackLayout
			{
				Children = { calibrationImage, calibrationImageButton }
			};

			await CreateHandlerAndAddToWindow(new ContentPage { Content = calibrationLayout }, () =>
			{
				Assert.NotNull(calibrationImage.Handler);
				Assert.NotNull(calibrationImageButton.Handler);
				Assert.IsAssignableFrom<UIView>(calibrationImage.Handler.PlatformView);
				Assert.IsAssignableFrom<UIView>(calibrationImageButton.Handler.PlatformView);

				expectedImageAlpha = GetNativeBackgroundAlpha(calibrationImage);
				expectedImageButtonAlpha = GetNativeBackgroundAlpha(calibrationImageButton);
				Assert.InRange(expectedImageAlpha, 0, ColorTolerance);
				Assert.InRange(expectedImageButtonAlpha, 0, ColorTolerance);
			});

			var image = new Image
			{
				Aspect = Aspect.AspectFit,
				BackgroundColor = Colors.Blue,
				HeightRequest = 150,
				Source = "red.png",
				WidthRequest = 150
			};
			var imageButton = new ImageButton
			{
				Aspect = Aspect.AspectFit,
				BackgroundColor = Colors.Blue,
				HeightRequest = 150,
				Source = "red.png",
				WidthRequest = 150
			};
			var setRedButton = new Button { Text = "Set both backgrounds red" };
			var clearButton = new Button { Text = "Clear both backgrounds" };
			var statusLabel = new Label { Text = "Background color state" };

			setRedButton.Clicked += (_, _) =>
			{
				image.BackgroundColor = Colors.Red;
				imageButton.BackgroundColor = Colors.Red;
			};
			clearButton.Clicked += (_, _) =>
			{
				image.BackgroundColor = null;
				imageButton.BackgroundColor = null;
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { FontSize = 18, Text = "Both controls start blue. Set them red, then clear their BackgroundColor." },
					new Label { Text = "Image" },
					image,
					new Label { Text = "ImageButton" },
					imageButton,
					setRedButton,
					clearButton,
					statusLabel
				}
			};
			var page = new ContentPage
			{
				Title = "Image BackgroundColor",
				Content = new ScrollView { Content = layout }
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				await image.WaitUntilLoaded();
				await AssertEventually(() => !imageButton.IsLoading, message: "ImageButton source did not finish loading.");

				var nativeImage = Assert.IsAssignableFrom<UIImageView>(image.Handler.PlatformView);
				var nativeImageButton = Assert.IsAssignableFrom<UIButton>(imageButton.Handler.PlatformView);
				Assert.NotNull(nativeImage.Image);
				Assert.NotNull(nativeImageButton.ImageForState(UIControlState.Normal));
				Assert.Equal(150, image.WidthRequest);
				Assert.Equal(150, image.HeightRequest);
				Assert.Equal(150, imageButton.WidthRequest);
				Assert.Equal(150, imageButton.HeightRequest);
				AssertNativeBackground(image, Colors.Blue);
				AssertNativeBackground(imageButton, Colors.Blue);

				setRedButton.SendClicked();
				await AssertEventually(
					() => NativeBackgroundMatches(image, Colors.Red),
					message: "Image native background did not change to red.");
				await AssertEventually(
					() => NativeBackgroundMatches(imageButton, Colors.Red),
					message: "ImageButton native background did not change to red.");

				int imageNullChange = -1;
				int imageButtonNullChange = -1;
				image.PropertyChanged += OnImagePropertyChanged;
				imageButton.PropertyChanged += OnImageButtonPropertyChanged;

				clearButton.SendClicked();

				Assert.Equal(1, imageNullChange);
				Assert.Equal(1, imageButtonNullChange);
				Assert.Null(image.BackgroundColor);
				Assert.Null(imageButton.BackgroundColor);

				double imageAlpha = GetNativeBackgroundAlpha(image);
				double imageButtonAlpha = GetNativeBackgroundAlpha(imageButton);
				bool backgroundsCleared =
					Math.Abs(imageAlpha - expectedImageAlpha) <= ColorTolerance &&
					Math.Abs(imageButtonAlpha - expectedImageButtonAlpha) <= ColorTolerance;

				Assert.True(
					backgroundsCleared,
					$"Native backgrounds were not cleared after BackgroundColor=null: " +
					$"Image observed alpha {imageAlpha:F3}, expected {expectedImageAlpha:F3}; " +
					$"ImageButton observed alpha {imageButtonAlpha:F3}, expected {expectedImageButtonAlpha:F3}.");

				void OnImagePropertyChanged(object sender, PropertyChangedEventArgs args)
				{
					if (args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName && image.BackgroundColor is null)
						imageNullChange = 1;
				}

				void OnImageButtonPropertyChanged(object sender, PropertyChangedEventArgs args)
				{
					if (args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName && imageButton.BackgroundColor is null)
						imageButtonNullChange = 1;
				}
			});
		}

		static double GetNativeBackgroundAlpha(VisualElement element)
		{
			var platformView = Assert.IsAssignableFrom<UIView>(element.Handler.PlatformView);
			var backgroundColor = platformView.BackgroundColor;
			if (backgroundColor is null)
				return 0;

			backgroundColor.GetRGBA(out _, out _, out _, out var alpha);
			return alpha;
		}

		static bool NativeBackgroundMatches(VisualElement element, Color expected)
		{
			var platformView = Assert.IsAssignableFrom<UIView>(element.Handler.PlatformView);
			var actual = platformView.BackgroundColor;
			if (actual is null)
				return false;

			actual.GetRGBA(out var actualRed, out var actualGreen, out var actualBlue, out var actualAlpha);
			expected.ToPlatform().GetRGBA(out var expectedRed, out var expectedGreen, out var expectedBlue, out var expectedAlpha);
			return Math.Abs((double)(actualRed - expectedRed)) <= ColorTolerance &&
				Math.Abs((double)(actualGreen - expectedGreen)) <= ColorTolerance &&
				Math.Abs((double)(actualBlue - expectedBlue)) <= ColorTolerance &&
				Math.Abs((double)(actualAlpha - expectedAlpha)) <= ColorTolerance;
		}

		static void AssertNativeBackground(VisualElement element, Color expected) =>
			Assert.True(NativeBackgroundMatches(element, expected), $"Native background did not match {expected}.");
	}
}
#endif

