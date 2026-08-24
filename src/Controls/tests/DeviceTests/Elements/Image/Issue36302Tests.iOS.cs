#if IOS && !MACCATALYST
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using Xunit;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Image, "Issue36302")]
	public class Issue36302 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ImageBackgroundColorResetsToTransparentWhenSetToNull()
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
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<Image, ImageHandler>();
				});
			});

			var defaultImage = new Image();
			await CreateHandlerAndAddToWindow(defaultImage, () =>
			{
				var defaultImageHandler = Assert.IsType<ImageHandler>(defaultImage.Handler);
				Assert.NotNull(defaultImageHandler.PlatformView);
				var defaultNativeAlpha = defaultImageHandler.PlatformView.BackgroundColor?.CGColor.Alpha ?? 0;

				Assert.InRange((double)defaultNativeAlpha, 0, 0.001);
			});

			var affectedImage = new Image
			{
				BackgroundColor = Colors.Blue,
				HeightRequest = 160,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Center
			};

			var layout = new VerticalStackLayout
			{
				Padding = 24,
				Spacing = 16,
				Children =
				{
					new Label
					{
						FontAttributes = FontAttributes.Bold,
						FontSize = 22,
						Text = "Image BackgroundColor null transition"
					},
					new Label
					{
						Text = "The Image starts blue, changes to red, and should become transparent when its BackgroundColor is set to null."
					},
					affectedImage,
					new Label { Text = "Image background: Blue" },
					new Label { Text = "Managed BackgroundColor: Blue" },
					new Button { Text = "Set runtime background to Red" },
					new Button { Text = "Set BackgroundColor to null" },
					new Button { Text = "Check rendered Image background" }
				}
			};

			var page = new ContentPage
			{
				Content = new ScrollView
				{
					Content = layout
				}
			};

			await CreateHandlerAndAddToWindow<WindowHandlerStub>(new Window(page), async _ =>
			{
				var imageHandler = Assert.IsType<ImageHandler>(affectedImage.Handler);
				var platformImage = imageHandler.PlatformView;

				Assert.True(platformImage.Frame.Width > 0 && platformImage.Frame.Height > 0);
				Assert.NotNull(platformImage.BackgroundColor);
				Assert.Equal(Colors.Blue, platformImage.BackgroundColor.ToColor());

				affectedImage.BackgroundColor = Colors.Red;
				await AssertEventually(
					() => platformImage.BackgroundColor is UIKit.UIColor nativeColor &&
						IsRed(nativeColor),
					message: "Image native background did not become red.");

				var clearNotificationCount = -1;
				affectedImage.PropertyChanged += (_, args) =>
				{
					if (args.PropertyName == VisualElement.BackgroundColorProperty.PropertyName &&
						affectedImage.BackgroundColor is null)
					{
						clearNotificationCount++;
					}
				};

				clearNotificationCount = 0;
				affectedImage.BackgroundColor = null;

				Assert.Equal(1, clearNotificationCount);
				Assert.Null(affectedImage.BackgroundColor);
				Assert.Same(platformImage, imageHandler.PlatformView);
				Assert.True(platformImage.Frame.Width > 0 && platformImage.Frame.Height > 0);

				double measuredAlpha = -1;
				var becameTransparent = await Wait(() =>
				{
					measuredAlpha = (double)(platformImage.BackgroundColor?.CGColor.Alpha ?? 0);
					return measuredAlpha <= 0.001;
				});

				Assert.True(
					becameTransparent,
					$"Issue 36302: Image native background should be transparent after BackgroundColor changes from Red to null. Measured alpha: {measuredAlpha}.");
			});
		}

		static bool IsRed(UIKit.UIColor color)
		{
			color.GetRGBA(out var red, out var green, out var blue, out var alpha);

			return red >= 0.999 && green <= 0.001 && blue <= 0.001 && alpha >= 0.999;
		}
	}
}
#endif

