using System.Threading.Tasks;
using Foundation;
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
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	public class Issue36302 : ControlsHandlerTestBase
	{
#if !MACCATALYST
		[Fact]
		public async Task SettingBackgroundColorToNullClearsNativeBackground()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
				});
			});

			var image = new Image
			{
				BackgroundColor = Colors.Blue,
				HeightRequest = 180,
				HorizontalOptions = LayoutOptions.Fill
			};
			var applyRedButton = new Button { Text = "Apply Red Background" };
			var clearBackgroundButton = new Button { Text = "Set BackgroundColor To Null" };
			var resultLabel = new Label
			{
				Text = "NO BUG:",
				FontSize = 18,
				FontAttributes = FontAttributes.Bold
			};

			applyRedButton.Clicked += (_, _) =>
			{
				image.BackgroundColor = Colors.Red;
				resultLabel.Text = "NO BUG:";
			};
			clearBackgroundButton.Clicked += (_, _) =>
			{
				image.BackgroundColor = null;
				resultLabel.Text = "NO BUG:";
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						Text = "Image BackgroundColor null reset",
						FontSize = 20,
						FontAttributes = FontAttributes.Bold
					},
					new Label { Text = "The image starts blue. Apply red, then clear the background to null." },
					image,
					applyRedButton,
					clearBackgroundButton,
					resultLabel
				}
			};
			var page = new ContentPage { Content = layout };
			var originalImage = image;
			Color observedBackground = Colors.Transparent;
			bool backgroundChanged = false;

			image.PropertyChanged += (_, args) =>
			{
				if (args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
				{
					observedBackground = image.BackgroundColor;
					backgroundChanged = true;
				}
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				Assert.True(NSThread.IsMain);
				Assert.Null(image.Source);
				Assert.Equal(180, image.HeightRequest);
				Assert.Equal(LayoutOptions.Fill, image.HorizontalOptions);
				Assert.Same(originalImage, layout.Children[2]);

				var originalHandler = Assert.IsType<ImageHandler>(image.Handler);
				var originalPlatformImage = Assert.IsAssignableFrom<UIImageView>(originalHandler.PlatformView);
				Assert.NotNull(originalPlatformImage.Window);
				Assert.True(originalPlatformImage.BackgroundColor.IsEqual(Colors.Blue.ToPlatform()));
				Assert.Equal(1d, (double)originalPlatformImage.BackgroundColor.CGColor.Alpha);

				var applyRedHandler = Assert.IsType<ButtonHandler>(applyRedButton.Handler);
				Assert.IsAssignableFrom<UIButton>(applyRedHandler.PlatformView)
					.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await AssertEventually(() => backgroundChanged, message: "Image.BackgroundColor did not change to red.");
				Assert.Equal(Colors.Red, observedBackground);
				Assert.Same(originalHandler, image.Handler);
				Assert.Same(originalPlatformImage, originalHandler.PlatformView);
				await AssertEventually(
					() => originalPlatformImage.BackgroundColor.IsEqual(Colors.Red.ToPlatform()),
					message: "The native Image background did not become red.");

				backgroundChanged = false;
				observedBackground = Colors.Transparent;

				var clearBackgroundHandler = Assert.IsType<ButtonHandler>(clearBackgroundButton.Handler);
				Assert.IsAssignableFrom<UIButton>(clearBackgroundHandler.PlatformView)
					.SendActionForControlEvents(UIControlEvent.TouchUpInside);

				await AssertEventually(() => backgroundChanged, message: "Image.BackgroundColor did not change to null.");
				Assert.Null(observedBackground);
				Assert.Same(originalImage, layout.Children[2]);
				Assert.Same(originalHandler, image.Handler);
				Assert.Same(originalPlatformImage, originalHandler.PlatformView);

				double observedNativeAlpha = -1;
				await AssertEventually(
					() =>
					{
						observedNativeAlpha = (double)(originalPlatformImage.BackgroundColor?.CGColor.Alpha ?? 0);
						return observedNativeAlpha == 0;
					},
					message: "The native Image background should be transparent after BackgroundColor is set to null.");
				Assert.Equal(0, observedNativeAlpha);
			});
		}
#endif
	}
}
