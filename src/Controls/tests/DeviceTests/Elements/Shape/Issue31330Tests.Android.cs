#if ANDROID
using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Layouts;
using Xunit;
using ABitmap = Android.Graphics.Bitmap;
using ACanvas = Android.Graphics.Canvas;
using AColor = Android.Graphics.Color;
using AView = Android.Views.View;

namespace Microsoft.Maui.DeviceTests
{
	[Collection(ControlsHandlerTestBase.RunInNewWindowCollection)]
	[Category(TestCategory.Shape)]
	[Category("Issue31330")]
	public class Issue31330 : ControlsHandlerTestBase
	{
		const double ShapeWidth = 20;
		const double ShapeHeight = 1.2;

		[Fact]
		public async Task RectangleBackgroundFillsFractionalHeight()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<AbsoluteLayout, LayoutHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
					handlers.AddHandler<Rectangle, RectangleHandler>();
				});
			});

			var shapeLayout = new AbsoluteLayout();
			var canvasGrid = new Grid
			{
				WidthRequest = 3370,
				HeightRequest = 2383,
				BackgroundColor = Colors.LightGray
			};
			canvasGrid.Children.Add(shapeLayout);

			var scrollView = new ScrollView
			{
				Orientation = ScrollOrientation.Both,
				VerticalScrollBarVisibility = ScrollBarVisibility.Always,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Always,
				Content = canvasGrid
			};

			var button = new Button
			{
				Text = "Add Rectangle",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Start
			};

			var mainLayout = new Grid();
			mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			mainLayout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });
			mainLayout.Children.Add(button);
			mainLayout.Children.Add(scrollView);
			Grid.SetRow(scrollView, 1);

			var page = new ContentPage { Content = mainLayout };
			var clicked = false;
			var loadedCount = -1;
			BoxView box = null;
			Rectangle rectangle = null;
			var boxBounds = new Rect(
				(3370 / 2d) - ShapeWidth - 50,
				(2383 / 2d) - (ShapeHeight / 2),
				ShapeWidth,
				ShapeHeight);
			var rectangleBounds = new Rect(
				(3370 / 2d) + 50,
				(2383 / 2d) - (ShapeHeight / 2),
				ShapeWidth,
				ShapeHeight);

			button.Clicked += (_, _) =>
			{
				clicked = true;
				box = new BoxView { BackgroundColor = Colors.Red };
				rectangle = new Rectangle { BackgroundColor = Colors.Blue };
				rectangle.Loaded += (_, _) => loadedCount = 1;

				AbsoluteLayout.SetLayoutBounds(box, boxBounds);
				AbsoluteLayout.SetLayoutFlags(box, AbsoluteLayoutFlags.None);
				AbsoluteLayout.SetLayoutBounds(rectangle, rectangleBounds);
				AbsoluteLayout.SetLayoutFlags(rectangle, AbsoluteLayoutFlags.None);

				shapeLayout.Children.Add(box);
				shapeLayout.Children.Add(rectangle);
			};

			await CreateHandlerAndAddToWindow(page, async () =>
			{
				var buttonHandler = Assert.IsType<ButtonHandler>(button.Handler);
				Assert.True(buttonHandler.PlatformView.PerformClick(), "The native button click was not handled.");
				Assert.True(clicked, "The Clicked callback did not run.");

				await AssertHelpers.AssertEventually(
					() => loadedCount == 1 &&
						box?.Handler?.PlatformView is AView boxView &&
						boxView.Width > 0 &&
						boxView.Height > 0 &&
						rectangle?.Handler?.PlatformView is AView rectangleView &&
						rectangleView.Width > 0 &&
						rectangleView.Height > 0,
					message: "The dynamically added shapes did not load and render.");

				Assert.Equal(2, shapeLayout.Children.Count);
				Assert.Same(box, shapeLayout.Children[0]);
				Assert.Same(rectangle, shapeLayout.Children[1]);
				Assert.Equal(boxBounds, AbsoluteLayout.GetLayoutBounds(box));
				Assert.Equal(rectangleBounds, AbsoluteLayout.GetLayoutBounds(rectangle));
				Assert.Equal(Colors.Red, box.BackgroundColor);
				Assert.Equal(Colors.Blue, rectangle.BackgroundColor);

				var boxHandler = Assert.IsType<BoxViewHandler>(box.Handler);
				var rectangleHandler = Assert.IsType<RectangleHandler>(rectangle.Handler);
				var boxPlatformView = boxHandler.PlatformView;
				var rectanglePlatformView = rectangleHandler.PlatformView;
				var density = rectanglePlatformView.Context.Resources.DisplayMetrics.Density;
				Assert.True(density > 0, "The active Android display density must be positive.");

				var expectedPixelHeight = (int)Math.Ceiling(ShapeHeight * density);
				Assert.InRange(boxPlatformView.Height, expectedPixelHeight - 1, expectedPixelHeight + 1);
				Assert.InRange(rectanglePlatformView.Height, expectedPixelHeight - 1, expectedPixelHeight + 1);
				Assert.Equal(boxPlatformView.Height, rectanglePlatformView.Height);

				using var boxBitmap = Capture(boxPlatformView);
				using var rectangleBitmap = Capture(rectanglePlatformView);
				var redHeight = GetMaximumConnectedColorHeight(boxBitmap, 255, 0, 0);
				var blueHeight = GetMaximumConnectedColorHeight(rectangleBitmap, 0, 0, 255);

				Assert.True(
					redHeight >= boxPlatformView.Height - 1,
					$"BoxView control pixels must cover its native frame; observed red height {redHeight}, native frame height {boxPlatformView.Height}.");
				Assert.True(
					blueHeight >= rectanglePlatformView.Height - 1,
					$"Rectangle fill height must cover its 1.2-DIP native frame; observed blue height {blueHeight}, expected height {expectedPixelHeight}, density {density}, native frame height {rectanglePlatformView.Height}.");
			});
		}

		static ABitmap Capture(AView view)
		{
			var bitmap = ABitmap.CreateBitmap(view.Width, view.Height, ABitmap.Config.Argb8888);
			Assert.NotNull(bitmap);
			using var canvas = new ACanvas(bitmap);
			view.Draw(canvas);
			return bitmap;
		}

		static int GetMaximumConnectedColorHeight(ABitmap bitmap, int red, int green, int blue)
		{
			const int colorTolerance = 24;
			var maximumHeight = 0;

			for (var x = 0; x < bitmap.Width; x++)
			{
				var currentHeight = 0;
				for (var y = 0; y < bitmap.Height; y++)
				{
					var pixel = bitmap.GetPixel(x, y);
					var matches =
						AColor.GetAlphaComponent(pixel) >= 200 &&
						Math.Abs(AColor.GetRedComponent(pixel) - red) <= colorTolerance &&
						Math.Abs(AColor.GetGreenComponent(pixel) - green) <= colorTolerance &&
						Math.Abs(AColor.GetBlueComponent(pixel) - blue) <= colorTolerance;

					currentHeight = matches ? currentHeight + 1 : 0;
					maximumHeight = Math.Max(maximumHeight, currentHeight);
				}
			}

			return maximumHeight;
		}
	}
}
#endif

