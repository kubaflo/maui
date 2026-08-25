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
#if IOS && !MACCATALYST
	[Category(TestCategory.Image)]
	[Category("Issue36302")]
	public class Issue36302 : ControlsHandlerTestBase
	{
		[Fact]
		[Description("Image and ImageButton native backgrounds should reset when BackgroundColor is set to null")]
		public async Task BackgroundColorsResetToTransparent()
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
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<ImageButton, ImageButtonHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var defaultImage = new Image { HeightRequest = 80, WidthRequest = 220 };
			var defaultImageButton = new ImageButton { HeightRequest = 80, WidthRequest = 220 };
			var defaultLayout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					defaultImage,
					defaultImageButton
				}
			};
			var defaultPage = new ContentPage
			{
				Content = new ScrollView { Content = defaultLayout }
			};

			await CreateHandlerAndAddToWindow(defaultPage, () =>
			{
				Assert.True(
					IsTransparent(GetNativeView(defaultImage).BackgroundColor),
					$"The default Image native background must be transparent, but was {DescribeColor(GetNativeView(defaultImage).BackgroundColor)}.");
				Assert.True(
					IsTransparent(GetNativeView(defaultImageButton).BackgroundColor),
					$"The default ImageButton native background must be transparent, but was {DescribeColor(GetNativeView(defaultImageButton).BackgroundColor)}.");
			});

			var image = new Image
			{
				BackgroundColor = Colors.Blue,
				HeightRequest = 80,
				WidthRequest = 220
			};
			var imageButton = new ImageButton
			{
				BackgroundColor = Colors.Blue,
				HeightRequest = 80,
				WidthRequest = 220
			};
			var setRedButton = new Button { Text = "Set backgrounds to red" };
			var clearButton = new Button { Text = "Clear backgrounds to null" };
			var checkButton = new Button { Text = "Check rendered backgrounds" };
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label { Text = "Image and ImageButton background reset", FontAttributes = FontAttributes.Bold, FontSize = 20 },
					new Label { Text = "Image" },
					image,
					new Label { Text = "ImageButton" },
					imageButton,
					setRedButton,
					clearButton,
					new Label { Text = "BackgroundColor: Blue" },
					checkButton,
					new Label { Text = "Native backgrounds: not checked" }
				}
			};
			var page = new ContentPage
			{
				Title = "Image background reset",
				Content = new ScrollView { Content = layout }
			};

			var imageNullTransitionCount = -1;
			var imageButtonNullTransitionCount = -1;
			var observeNullTransition = false;
			image.PropertyChanged += (_, args) =>
			{
				if (observeNullTransition &&
					args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName &&
					image.BackgroundColor is null)
					imageNullTransitionCount = imageNullTransitionCount < 0 ? 1 : imageNullTransitionCount + 1;
			};
			imageButton.PropertyChanged += (_, args) =>
			{
				if (observeNullTransition &&
					args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName &&
					imageButton.BackgroundColor is null)
					imageButtonNullTransitionCount = imageButtonNullTransitionCount < 0 ? 1 : imageButtonNullTransitionCount + 1;
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var nativeImage = Assert.IsAssignableFrom<UIImageView>(Assert.IsType<ImageHandler>(image.Handler).PlatformView);
				var nativeImageButton = Assert.IsAssignableFrom<UIButton>(Assert.IsType<ImageButtonHandler>(imageButton.Handler).PlatformView);

				Assert.Equal(220, image.Width);
				Assert.Equal(80, image.Height);
				Assert.Equal(220, imageButton.Width);
				Assert.Equal(80, imageButton.Height);
				await AssertEventually(
					() => IsOpaqueColor(nativeImage.BackgroundColor, red: 0, green: 0, blue: 1) &&
						IsOpaqueColor(nativeImageButton.BackgroundColor, red: 0, green: 0, blue: 1),
					message: "Image and ImageButton did not render their initial opaque blue backgrounds.");

				image.BackgroundColor = Colors.Red;
				imageButton.BackgroundColor = Colors.Red;
				await AssertEventually(
					() => IsOpaqueColor(nativeImage.BackgroundColor, red: 1, green: 0, blue: 0) &&
						IsOpaqueColor(nativeImageButton.BackgroundColor, red: 1, green: 0, blue: 0),
					message: "Image and ImageButton did not render their runtime opaque red backgrounds.");

				var originalNativeImage = nativeImage;
				var originalNativeImageButton = nativeImageButton;
				observeNullTransition = true;
				image.BackgroundColor = null;
				imageButton.BackgroundColor = null;

				Assert.True(imageNullTransitionCount > 0, "Image did not report the managed null BackgroundColor transition.");
				Assert.True(imageButtonNullTransitionCount > 0, "ImageButton did not report the managed null BackgroundColor transition.");
				Assert.Same(originalNativeImage, GetNativeView(image));
				Assert.Same(originalNativeImageButton, GetNativeView(imageButton));

				UIColor observedImageBackground = UIColor.Magenta;
				UIColor observedImageButtonBackground = UIColor.Magenta;
				var imageBecameTransparent = await Wait(
					() =>
					{
						observedImageBackground = nativeImage.BackgroundColor;
						return IsTransparent(observedImageBackground);
					});
				var imageButtonBecameTransparent = await Wait(
					() =>
					{
						observedImageButtonBackground = nativeImageButton.BackgroundColor;
						return IsTransparent(observedImageButtonBackground);
					});

				Assert.True(
					imageBecameTransparent && imageButtonBecameTransparent,
					$"Image or ImageButton native background remained opaque after BackgroundColor changed to null; expected both transparent, observed Image={DescribeColor(observedImageBackground)}, ImageButton={DescribeColor(observedImageButtonBackground)}.");
			});
		}

		static UIView GetNativeView(View control)
		{
			if (control is Image image)
				return Assert.IsAssignableFrom<UIImageView>(Assert.IsType<ImageHandler>(image.Handler).PlatformView);

			var imageButton = Assert.IsType<ImageButton>(control);
			return Assert.IsAssignableFrom<UIButton>(Assert.IsType<ImageButtonHandler>(imageButton.Handler).PlatformView);
		}

		static bool IsTransparent(UIColor color)
		{
			if (color is null)
				return true;

			color.GetRGBA(out _, out _, out _, out var alpha);
			return alpha <= 0.01;
		}

		static bool IsOpaqueColor(UIColor color, double red, double green, double blue)
		{
			if (color is null)
				return false;

			color.GetRGBA(out var actualRed, out var actualGreen, out var actualBlue, out var alpha);
			return Math.Abs((double)actualRed - red) <= 0.01 &&
				Math.Abs((double)actualGreen - green) <= 0.01 &&
				Math.Abs((double)actualBlue - blue) <= 0.01 &&
				alpha >= 0.99;
		}

		static string DescribeColor(UIColor color)
		{
			if (color is null)
				return "null";

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return $"RGBA({red:F3}, {green:F3}, {blue:F3}, {alpha:F3})";
		}
	}
#endif
}

