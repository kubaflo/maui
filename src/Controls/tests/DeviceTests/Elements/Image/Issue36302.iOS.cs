#if IOS && !MACCATALYST
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
	public class Issue36302 : ControlsHandlerTestBase
	{
		[Fact]
		public async Task ClearingBackgroundColorRestoresNativeDefault()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Controls.Window, WindowHandlerStub>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<ImageButton, ImageButtonHandler>();
				});
			});

			var defaultImageButton = CreateImageButton();
			UIColor defaultBackgroundColor = null;

			await CreateHandlerAndAddToWindow<IWindowHandler>(CreatePage(defaultImageButton), _ =>
			{
				var defaultHandler = Assert.IsType<ImageButtonHandler>(defaultImageButton.Handler);
				var defaultPlatformButton = defaultHandler.PlatformView;

				Assert.Equal(140d, defaultPlatformButton.Frame.Width, 1);
				Assert.Equal(140d, defaultPlatformButton.Frame.Height, 1);
				defaultBackgroundColor = defaultPlatformButton.BackgroundColor;
				Assert.False(AreEquivalent(defaultBackgroundColor, UIColor.Red), "The clean native default must not be red");
				return Task.CompletedTask;
			});

			var imageButton = CreateImageButton();
			imageButton.BackgroundColor = Colors.Blue;

			await CreateHandlerAndAddToWindow<IWindowHandler>(CreatePage(imageButton), async _ =>
			{
				var imageButtonHandler = Assert.IsType<ImageButtonHandler>(imageButton.Handler);
				var platformButton = imageButtonHandler.PlatformView;

				Assert.Equal(140d, platformButton.Frame.Width, 1);
				Assert.Equal(140d, platformButton.Frame.Height, 1);
				await AssertEventually(
					() => AreEquivalent(platformButton.BackgroundColor, Colors.Blue.ToPlatform()),
					message: "ImageButton native background did not become blue");

				imageButton.BackgroundColor = Colors.Red;
				await AssertEventually(
					() => AreEquivalent(platformButton.BackgroundColor, Colors.Red.ToPlatform()),
					message: "ImageButton native background did not become red");

				bool backgroundColorTransitionObserved = false;
				PropertyChangedEventHandler propertyChanged = (_, args) =>
				{
					if (args.PropertyName == nameof(ImageButton.BackgroundColor) && imageButton.BackgroundColor is null)
						backgroundColorTransitionObserved = true;
				};

				imageButton.PropertyChanged += propertyChanged;
				imageButton.BackgroundColor = null;

				await AssertEventually(
					() => backgroundColorTransitionObserved,
					message: "The BackgroundColor property transition to null was not observed");
				imageButton.PropertyChanged -= propertyChanged;
				Assert.Null(imageButton.BackgroundColor);

				await AssertEventually(
					() => AreEquivalent(platformButton.BackgroundColor, defaultBackgroundColor),
					message: $"ImageButton native background did not reset after BackgroundColor became null. Expected {Format(defaultBackgroundColor)}, actual {Format(platformButton.BackgroundColor)}");
			});
		}

		static ImageButton CreateImageButton() =>
			new()
			{
				Aspect = Aspect.AspectFit,
				HeightRequest = 140,
				Source = "dotnet_bot.png",
				WidthRequest = 140,
			};

		static ContentPage CreatePage(ImageButton imageButton)
		{
			var grid = new Grid
			{
				BackgroundColor = Colors.White,
				HeightRequest = 150,
				WidthRequest = 150,
			};
			grid.Add(imageButton);

			return new ContentPage
			{
				BackgroundColor = Colors.White,
				Content = new ScrollView
				{
					Content = new VerticalStackLayout
					{
						Padding = 12,
						Spacing = 8,
						Children = { grid },
					},
				},
			};
		}

		static bool AreEquivalent(UIColor first, UIColor second) =>
			ColorComparison.ARGBEquivalent(first, second, tolerance: 0.001);

		static string Format(UIColor color)
		{
			if (color is null)
				return "null";

			color.GetRGBA(out var red, out var green, out var blue, out var alpha);
			return $"RGBA({red:F3}, {green:F3}, {blue:F3}, {alpha:F3})";
		}
	}
}
#endif

