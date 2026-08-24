using System.ComponentModel;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
#if IOS && !MACCATALYST
	[Category(TestCategory.Image)]
	[Category("Issue36302")]
	public class Issue36302 : CoreHandlerTestBase
	{
		[Fact]
		public async Task ClearingBackgroundColorRestoresNativeDefault()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Window, WindowHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<ImageButton, ImageButtonHandler>();
				});
			});

			var defaultImage = CreateImage();
			var defaultImageButton = CreateImageButton();
			var defaultImageBackground = UIColor.Magenta;
			var defaultImageButtonBackground = UIColor.Magenta;
			var defaultsCaptured = false;

			await AttachAndRun(CreatePage(defaultImage, defaultImageButton), async _ =>
			{
				var defaultImageHandler = Assert.IsType<ImageHandler>(defaultImage.Handler);
				var defaultImageButtonHandler = Assert.IsType<ImageButtonHandler>(defaultImageButton.Handler);

				await AssertSourcesLoaded(defaultImageHandler, defaultImageButtonHandler);

				defaultImageBackground = GetImageBackground(defaultImageHandler);
				defaultImageButtonBackground = GetImageButtonBackground(defaultImageButtonHandler);
				Assert.True(IsClear(defaultImageBackground),
					$"Clean Image native background was not clear: {Describe(defaultImageBackground)}");
				Assert.True(IsClear(defaultImageButtonBackground),
					$"Clean ImageButton native background was not clear: {Describe(defaultImageButtonBackground)}");
				defaultsCaptured = true;
			});

			Assert.True(defaultsCaptured, "Clean native background defaults were not captured.");

			var image = CreateImage(Colors.Blue);
			var imageButton = CreateImageButton(Colors.Blue);

			await AttachAndRun(CreatePage(image, imageButton), async _ =>
			{
				var imageHandler = Assert.IsType<ImageHandler>(image.Handler);
				var imageButtonHandler = Assert.IsType<ImageButtonHandler>(imageButton.Handler);

				await AssertSourcesLoaded(imageHandler, imageButtonHandler);
				Assert.Equal(Colors.Blue, image.BackgroundColor);
				Assert.Equal(Colors.Blue, imageButton.BackgroundColor);

				image.BackgroundColor = Colors.Red;
				imageButton.BackgroundColor = Colors.Red;
				await AssertNativeColors(imageHandler, imageButtonHandler, Colors.Red, "runtime red");

				var imageBackgroundCleared = false;
				var imageButtonBackgroundCleared = false;

				image.PropertyChanged += OnImagePropertyChanged;
				imageButton.PropertyChanged += OnImageButtonPropertyChanged;

				image.BackgroundColor = null;
				imageButton.BackgroundColor = null;

				await AssertEventually(
					() => imageBackgroundCleared,
					message: "Image did not report the BackgroundColor null transition.");
				await AssertEventually(
					() => imageButtonBackgroundCleared,
					message: "ImageButton did not report the BackgroundColor null transition.");

				Assert.Null(image.BackgroundColor);
				Assert.Null(imageButton.BackgroundColor);

				var imageRestored = await Wait(
					() => ColorComparison.ARGBEquivalent(GetImageBackground(imageHandler), defaultImageBackground));
				var imageButtonRestored = await Wait(
					() => ColorComparison.ARGBEquivalent(GetImageButtonBackground(imageButtonHandler), defaultImageButtonBackground));

				var actualImageBackground = GetImageBackground(imageHandler);
				var actualImageButtonBackground = GetImageButtonBackground(imageButtonHandler);

				Assert.True(imageRestored,
					$"Image: native background remained non-default after BackgroundColor was cleared. Expected {Describe(defaultImageBackground)}, actual {Describe(actualImageBackground)}.");
				Assert.True(imageButtonRestored,
					$"ImageButton: native background remained non-default after BackgroundColor was cleared. Expected {Describe(defaultImageButtonBackground)}, actual {Describe(actualImageButtonBackground)}.");

				void OnImagePropertyChanged(object sender, PropertyChangedEventArgs args)
				{
					if (args.PropertyName == nameof(VisualElement.BackgroundColor) && image.BackgroundColor is null)
						imageBackgroundCleared = true;
				}

				void OnImageButtonPropertyChanged(object sender, PropertyChangedEventArgs args)
				{
					if (args.PropertyName == nameof(VisualElement.BackgroundColor) && imageButton.BackgroundColor is null)
						imageButtonBackgroundCleared = true;
				}
			});
		}

		static Image CreateImage(Color backgroundColor = null) =>
			new()
			{
				Aspect = Aspect.AspectFit,
				BackgroundColor = backgroundColor,
				Source = "dotnet_bot.svg"
			};

		static ImageButton CreateImageButton(Color backgroundColor = null) =>
			new()
			{
				Aspect = Aspect.AspectFit,
				BackgroundColor = backgroundColor,
				Source = "dotnet_bot.svg"
			};

		static ContentPage CreatePage(Image image, ImageButton imageButton)
		{
			var imageHost = new Grid
			{
				BackgroundColor = Colors.Lime,
				HeightRequest = 120
			};
			imageHost.Add(image);

			var imageButtonHost = new Grid
			{
				BackgroundColor = Colors.Lime,
				HeightRequest = 120
			};
			imageButtonHost.Add(imageButton);

			return new ContentPage
			{
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 24,
						Spacing = 16,
						Children =
						{
							new Label { Text = "Image" },
							imageHost,
							new Label { Text = "ImageButton" },
							imageButtonHost
						}
					}
				}
			};
		}

		static async Task AssertSourcesLoaded(ImageHandler imageHandler, ImageButtonHandler imageButtonHandler)
		{
			await AssertEventually(
				() => imageHandler.PlatformView.Image is not null,
				timeout: 5000,
				message: "Image did not load dotnet_bot.svg through the file-image source service.");
			await AssertEventually(
				() => imageButtonHandler.PlatformView.ImageForState(UIControlState.Normal) is not null,
				timeout: 5000,
				message: "ImageButton did not load dotnet_bot.svg through the file-image source service.");
		}

		static async Task AssertNativeColors(
			ImageHandler imageHandler,
			ImageButtonHandler imageButtonHandler,
			Color expected,
			string stage)
		{
			var expectedPlatformColor = expected.ToPlatform();

			await AssertEventually(
				() => ColorComparison.ARGBEquivalent(GetImageBackground(imageHandler), expectedPlatformColor),
				message: $"Image native background did not become {stage}. Actual: {Describe(GetImageBackground(imageHandler))}.");
			await AssertEventually(
				() => ColorComparison.ARGBEquivalent(GetImageButtonBackground(imageButtonHandler), expectedPlatformColor),
				message: $"ImageButton native background did not become {stage}. Actual: {Describe(GetImageButtonBackground(imageButtonHandler))}.");
		}

		static UIColor GetImageBackground(ImageHandler handler) =>
			GetEffectiveBackground(handler.PlatformView, handler.ContainerView);

		static UIColor GetImageButtonBackground(ImageButtonHandler handler) =>
			GetEffectiveBackground(handler.PlatformView, handler.ContainerView);

		static UIColor GetEffectiveBackground(UIView platformView, UIView containerView)
		{
			if (!IsClear(platformView.BackgroundColor))
				return platformView.BackgroundColor;

			return containerView?.BackgroundColor;
		}

		static bool IsClear(UIColor color)
		{
			if (color is null)
				return true;

			color.GetRGBA(out _, out _, out _, out var alpha);
			return alpha <= 0.000001;
		}

		static string Describe(UIColor color)
		{
			if (color is null)
				return "<null>";

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return $"RGBA({red:F6}, {green:F6}, {blue:F6}, {alpha:F6})";
		}
	}
#endif
}

