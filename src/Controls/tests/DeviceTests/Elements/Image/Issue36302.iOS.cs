#if IOS && !MACCATALYST
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using UIKit;
using Xunit;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Image)]
	public class Issue36302 : ControlsHandlerTestBase
	{
		const string FailureSignature = "Image native background remained opaque after BackgroundColor was set to null";

		[Category("Issue36302")]
		[Fact]
		public async Task ClearingImageBackgroundColorClearsNativeBackground()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Image, ImageHandler>();
				});
			});

			var defaultImage = new Image
			{
				HeightRequest = 180,
				WidthRequest = 180,
				HorizontalOptions = LayoutOptions.Center
			};

			var defaultPage = CreatePage(defaultImage);

			await CreateHandlerAndAddToWindow(defaultPage, () =>
			{
				var defaultHandler = Assert.IsType<ImageHandler>(defaultImage.Handler);
				var defaultPlatformView = Assert.IsAssignableFrom<UIImageView>(defaultHandler.PlatformView);

				if (defaultHandler.ContainerView is UIView defaultContainer)
					AssertTransparent(defaultContainer, "Default Image container was expected to be transparent");

				AssertTransparent(defaultPlatformView, "Default UIImageView was expected to be transparent");
			});

			var image = new Image
			{
				BackgroundColor = Colors.Blue,
				HeightRequest = 180,
				WidthRequest = 180,
				HorizontalOptions = LayoutOptions.Center
			};

			var page = CreatePage(image);

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var imageHandler = Assert.IsType<ImageHandler>(image.Handler);
				var platformView = Assert.IsAssignableFrom<UIImageView>(imageHandler.PlatformView);

				Assert.Same(image, imageHandler.VirtualView);
				Assert.True(platformView.Frame.Width > 0 && platformView.Frame.Height > 0, "The attached UIImageView was not laid out.");
				Assert.InRange(platformView.Frame.Width, 179, 181);
				Assert.InRange(platformView.Frame.Height, 179, 181);
				Assert.True(
					IsOpaqueColor(imageHandler.ContainerView, UIColor.Blue) || IsOpaqueColor(platformView, UIColor.Blue),
					"The attached Image did not initially render an opaque blue background.");

				image.BackgroundColor = Colors.Red;

				Assert.True(
					IsOpaqueColor(imageHandler.ContainerView, UIColor.Red) || IsOpaqueColor(platformView, UIColor.Red),
					"The attached Image did not render an opaque red background after the runtime property change.");

				var transitionCompletion = new TaskCompletionSource();
				var transitionObserved = false;
				object observedBackground = new object();

				image.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
					{
						transitionObserved = true;
						observedBackground = image.BackgroundColor;
						transitionCompletion.TrySetResult();
					}
				};

				image.BackgroundColor = null;

				await transitionCompletion.Task.WaitAsync(TimeSpan.FromSeconds(1));

				Assert.True(transitionObserved, "The post-trigger BackgroundColor property transition was not observed.");
				Assert.Null(observedBackground);
				Assert.Null(image.BackgroundColor);
				Assert.Same(imageHandler, image.Handler);
				Assert.Same(platformView, imageHandler.PlatformView);

				if (imageHandler.ContainerView is UIView containerView)
					AssertTransparent(containerView, $"{FailureSignature}: native container");

				AssertTransparent(platformView, $"{FailureSignature}: UIImageView");
			});
		}

		static ContentPage CreatePage(Image image)
		{
			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16
			};
			layout.Add(image);

			return new ContentPage
			{
				BackgroundColor = Colors.White,
				Content = layout
			};
		}

		static bool IsOpaqueColor(UIView view, UIColor expectedColor)
		{
			if (view?.BackgroundColor is not UIColor actualColor)
				return false;

			var actual = GetRgba(actualColor);
			var expected = GetRgba(expectedColor);

			return actual.Alpha >= 0.99 &&
				Math.Abs(actual.Red - expected.Red) <= 0.01 &&
				Math.Abs(actual.Green - expected.Green) <= 0.01 &&
				Math.Abs(actual.Blue - expected.Blue) <= 0.01;
		}

		static void AssertTransparent(UIView view, string message)
		{
			var rgba = GetRgba(view.BackgroundColor);

			Assert.True(
				rgba.Alpha <= 0.01,
				$"{message} observed RGBA ({rgba.Red:F3}, {rgba.Green:F3}, {rgba.Blue:F3}, {rgba.Alpha:F3}); expected alpha <= 0.01.");
		}

		static (nfloat Red, nfloat Green, nfloat Blue, nfloat Alpha) GetRgba(UIColor color)
		{
			if (color is null)
				return (0, 0, 0, 0);

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return (red, green, blue, alpha);
		}
	}
}
#endif

