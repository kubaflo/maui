using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Xunit;
using ABitmap = Android.Graphics.Bitmap;
using ACanvas = Android.Graphics.Canvas;
using AView = Android.Views.View;
using AbsoluteLayoutFlags = Microsoft.Maui.Layouts.AbsoluteLayoutFlags;
using Rectangle = Microsoft.Maui.Controls.Shapes.Rectangle;
using static Microsoft.Maui.DeviceTests.AssertHelpers;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Shape)]
	[Category("Issue31330")]
	[Collection(RunInNewWindowCollection)]
	public class Issue31330 : ControlsHandlerTestBase
	{
		const double CanvasWidth = 3370;
		const double CanvasHeight = 2383;
		const double ShapeWidth = 20;
		const double ShapeHeight = 1.2;

#if ANDROID
		[Fact]
		public async Task FractionalHeightRectangleFillsMeasuredNativeFrame()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<AbsoluteLayout, LayoutHandler>();
					handlers.AddHandler<ScrollView, ScrollViewHandler>();
					handlers.AddHandler<Button, ButtonHandler>();
					handlers.AddHandler<BoxView, BoxViewHandler>();
					handlers.AddHandler<Rectangle, RectangleHandler>();
				});
			});

			var shapeLayout = new AbsoluteLayout();
			var canvas = new Grid
			{
				WidthRequest = CanvasWidth,
				HeightRequest = CanvasHeight,
				BackgroundColor = Colors.LightGray
			};
			canvas.Children.Add(shapeLayout);

			var scrollView = new ScrollView
			{
				Orientation = ScrollOrientation.Both,
				VerticalScrollBarVisibility = ScrollBarVisibility.Always,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Always,
				Content = canvas
			};
			var button = new Button
			{
				Text = "Add Rectangle",
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Start
			};
			var mainLayout = new Grid
			{
				RowDefinitions =
				{
					new RowDefinition(GridLength.Auto),
					new RowDefinition(GridLength.Star)
				}
			};
			mainLayout.Add(button, 0, 0);
			mainLayout.Add(scrollView, 0, 1);

			var page = new ContentPage { Content = mainLayout };
			var clickState = -1;
			BoxView addedBox = null;
			Rectangle addedRectangle = null;

			var expectedBoxBounds = new Rect(
				(CanvasWidth / 2) - ShapeWidth - 50,
				(CanvasHeight / 2) - (ShapeHeight / 2),
				ShapeWidth,
				ShapeHeight);
			var expectedRectangleBounds = new Rect(
				(CanvasWidth / 2) + 50,
				(CanvasHeight / 2) - (ShapeHeight / 2),
				ShapeWidth,
				ShapeHeight);

			button.Clicked += (_, _) =>
			{
				addedBox = new BoxView { BackgroundColor = Colors.Red };
				AbsoluteLayout.SetLayoutBounds(addedBox, expectedBoxBounds);
				AbsoluteLayout.SetLayoutFlags(addedBox, AbsoluteLayoutFlags.None);

				addedRectangle = new Rectangle { BackgroundColor = Colors.Blue };
				AbsoluteLayout.SetLayoutBounds(addedRectangle, expectedRectangleBounds);
				AbsoluteLayout.SetLayoutFlags(addedRectangle, AbsoluteLayoutFlags.None);

				shapeLayout.Children.Add(addedBox);
				shapeLayout.Children.Add(addedRectangle);
				clickState = 1;
			};

			await AttachAndRun<PageHandler>(page, async _ =>
			{
				var buttonHandler = Assert.IsType<ButtonHandler>(button.Handler);
				buttonHandler.PlatformView.PerformClick();

				Assert.Equal(1, clickState);
				Assert.Equal(2, shapeLayout.Children.Count);
				Assert.Same(addedBox, shapeLayout.Children[0]);
				Assert.Same(addedRectangle, shapeLayout.Children[1]);
				Assert.Equal(expectedBoxBounds, AbsoluteLayout.GetLayoutBounds(addedBox));
				Assert.Equal(expectedRectangleBounds, AbsoluteLayout.GetLayoutBounds(addedRectangle));

				await AssertEventually(() =>
					addedBox.Handler is BoxViewHandler boxHandler &&
					addedRectangle.Handler is RectangleHandler rectangleHandler &&
					boxHandler.PlatformView.Width > 0 &&
					boxHandler.PlatformView.Height > 0 &&
					rectangleHandler.PlatformView.Width > 0 &&
					rectangleHandler.PlatformView.Height > 0 &&
					boxHandler.PlatformView.Drawable is not null &&
					rectangleHandler.PlatformView.Drawable is not null &&
					Math.Abs(addedBox.Bounds.X - expectedBoxBounds.X) < 0.01 &&
					Math.Abs(addedBox.Bounds.Y - expectedBoxBounds.Y) < 0.01 &&
					Math.Abs(addedRectangle.Bounds.X - expectedRectangleBounds.X) < 0.01 &&
					Math.Abs(addedRectangle.Bounds.Y - expectedRectangleBounds.Y) < 0.01);

				var nativeBox = Assert.IsType<BoxViewHandler>(addedBox.Handler).PlatformView;
				var nativeRectangle = Assert.IsType<RectangleHandler>(addedRectangle.Handler).PlatformView;
				var density = nativeBox.Context.Resources.DisplayMetrics.Density;
				var minimumNativeWidth = (int)Math.Floor(ShapeWidth * density);
				var maximumNativeWidth = (int)Math.Ceiling(ShapeWidth * density);
				var minimumNativeHeight = Math.Max(1, (int)Math.Floor(ShapeHeight * density));
				var maximumNativeHeight = Math.Max(1, (int)Math.Ceiling(ShapeHeight * density));

				Assert.InRange(nativeBox.Width, minimumNativeWidth, maximumNativeWidth);
				Assert.InRange(nativeBox.Height, minimumNativeHeight, maximumNativeHeight);
				Assert.InRange(nativeRectangle.Width, minimumNativeWidth, maximumNativeWidth);
				Assert.InRange(nativeRectangle.Height, minimumNativeHeight, maximumNativeHeight);
				Assert.Equal(nativeBox.MeasuredWidth, nativeBox.Width);
				Assert.Equal(nativeBox.MeasuredHeight, nativeBox.Height);
				Assert.Equal(nativeRectangle.MeasuredWidth, nativeRectangle.Width);
				Assert.Equal(nativeRectangle.MeasuredHeight, nativeRectangle.Height);

				var boxCoverage = MeasureFillCoverage(nativeBox, Colors.Red);
				var rectangleCoverage = MeasureFillCoverage(nativeRectangle, Colors.Blue);
				var expectedBoxRows = Math.Max(1, boxCoverage.Height - 1);
				var expectedBoxPixels = boxCoverage.Width * expectedBoxRows;
				var expectedRectangleRows = Math.Max(1, rectangleCoverage.Height - 1);
				var expectedRectanglePixels = rectangleCoverage.Width * expectedRectangleRows;

				Assert.True(
					boxCoverage.FilledRows >= expectedBoxRows && boxCoverage.FilledPixels >= expectedBoxPixels,
					$"BoxView fill coverage must occupy its measured native frame. Observed {boxCoverage.FilledRows}/{boxCoverage.Height} rows and {boxCoverage.FilledPixels}/{boxCoverage.Width * boxCoverage.Height} pixels; expected at least {expectedBoxRows} rows and {expectedBoxPixels} pixels.");
				Assert.True(
					rectangleCoverage.FilledRows >= expectedRectangleRows && rectangleCoverage.FilledPixels >= expectedRectanglePixels,
					$"Rectangle fill coverage must occupy its measured native frame. Observed {rectangleCoverage.FilledRows}/{rectangleCoverage.Height} rows and {rectangleCoverage.FilledPixels}/{rectangleCoverage.Width * rectangleCoverage.Height} pixels; expected at least {expectedRectangleRows} rows and {expectedRectanglePixels} pixels.");
			});
		}
#endif

		static (int Width, int Height, int FilledRows, int FilledPixels) MeasureFillCoverage(AView view, Color expectedColor)
		{
			using var bitmap = ABitmap.CreateBitmap(view.Width, view.Height, ABitmap.Config.Argb8888);
			using var canvas = new ACanvas(bitmap);
			view.Draw(canvas);

			var pixels = new int[bitmap.Width * bitmap.Height];
			bitmap.GetPixels(pixels, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);

			var expectedRed = (int)Math.Round(expectedColor.Red * 255);
			var expectedGreen = (int)Math.Round(expectedColor.Green * 255);
			var expectedBlue = (int)Math.Round(expectedColor.Blue * 255);
			var filledRows = 0;
			var filledPixels = 0;

			for (var row = 0; row < bitmap.Height; row++)
			{
				var rowContainsFill = false;
				for (var column = 0; column < bitmap.Width; column++)
				{
					var pixel = pixels[(row * bitmap.Width) + column];
					var alpha = (pixel >> 24) & 0xFF;
					var red = (pixel >> 16) & 0xFF;
					var green = (pixel >> 8) & 0xFF;
					var blue = pixel & 0xFF;
					if (alpha >= 240 &&
						Math.Abs(red - expectedRed) <= 8 &&
						Math.Abs(green - expectedGreen) <= 8 &&
						Math.Abs(blue - expectedBlue) <= 8)
					{
						filledPixels++;
						rowContainsFill = true;
					}
				}

				if (rowContainsFill)
					filledRows++;
			}

			return (bitmap.Width, bitmap.Height, filledRows, filledPixels);
		}
	}
}

