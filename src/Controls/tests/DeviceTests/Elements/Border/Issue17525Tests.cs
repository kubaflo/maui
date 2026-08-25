#if MACCATALYST
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.DeviceTests.Stubs;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Platform;
using UIKit;
using Xunit;
using MContentView = Microsoft.Maui.Platform.ContentView;

namespace Microsoft.Maui.DeviceTests
{
	[Category(TestCategory.Border)]
	public class Issue17525 : ControlsHandlerTestBase
	{
		[Fact]
		[Category("Issue17525")]
		public async Task PolygonContentIsClippedInsideStroke()
		{
			EnsureHandlerCreated(builder =>
			{
				builder.ConfigureMauiHandlers(handlers =>
				{
					handlers.AddHandler<Window, WindowHandlerStub>();
					handlers.AddHandler<Page, PageHandler>();
					handlers.AddHandler<ContentPage, PageHandler>();
					handlers.AddHandler<Layout, LayoutHandler>();
					handlers.AddHandler<Grid, LayoutHandler>();
					handlers.AddHandler<VerticalStackLayout, LayoutHandler>();
					handlers.AddHandler<Border, BorderHandler>();
					handlers.AddHandler<Label, LabelHandler>();
					handlers.AddHandler<Image, ImageHandler>();
					handlers.AddHandler<Entry, EntryHandler>();
					handlers.AddHandler<Slider, SliderHandler>();
				});
			});

			Label CreateContentLabel() => new()
			{
				BackgroundColor = Color.FromArgb("#99FF0000"),
				FontSize = 40,
				HorizontalTextAlignment = TextAlignment.Center,
				VerticalTextAlignment = TextAlignment.Center,
				HorizontalOptions = LayoutOptions.Center,
				VerticalOptions = LayoutOptions.Center,
				Text = "+",
				TextColor = Color.FromArgb("#0088EE")
			};

			Image CreateContentImage() => new()
			{
				Source = "oasis.jpg",
				Scale = 1
			};

			Polygon CreateTriangle() => new()
			{
				Points = new PointCollection
				{
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50)
				},
				StrokeThickness = 3
			};

			Border AddBorder(Grid grid, int column, int row, Shape shape, View content)
			{
				var border = new Border
				{
					StrokeShape = shape,
					WidthRequest = 101,
					HeightRequest = 101,
					BackgroundColor = Colors.LightBlue,
					StrokeThickness = 8,
					Stroke = Colors.LightGreen,
					Content = content
				};

				Grid.SetColumn(border, column);
				Grid.SetRow(border, row);
				grid.Children.Add(border);
				return border;
			}

			var grid = new Grid
			{
				ColumnDefinitions =
				{
					new ColumnDefinition(GridLength.Star),
					new ColumnDefinition(GridLength.Star)
				},
				RowDefinitions =
				{
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Star)
				},
				ColumnSpacing = 10,
				RowSpacing = 10,
				VerticalOptions = LayoutOptions.Center
			};

			var circleLabel = CreateContentLabel();
			var roundRectangleLabel = CreateContentLabel();
			var triangleLabel = CreateContentLabel();
			var circleImage = CreateContentImage();
			var roundRectangleImage = CreateContentImage();
			var triangleImage = CreateContentImage();

			var circleLabelBorder = AddBorder(grid, 0, 0, new Ellipse(), circleLabel);
			var roundRectangleBorder = AddBorder(grid, 0, 1, new RoundRectangle(), roundRectangleLabel);
			var triangleBorder = AddBorder(grid, 0, 2, CreateTriangle(), triangleLabel);
			var circleImageBorder = AddBorder(grid, 1, 0, new Ellipse(), circleImage);
			var roundRectangleImageBorder = AddBorder(grid, 1, 1, new RoundRectangle(), roundRectangleImage);
			var triangleImageBorder = AddBorder(grid, 1, 2, CreateTriangle(), triangleImage);
			var borders = new[]
			{
				circleLabelBorder,
				roundRectangleBorder,
				triangleBorder,
				circleImageBorder,
				roundRectangleImageBorder,
				triangleImageBorder
			};

			var textEntry = new Entry { Text = "+" };
			var fontSizeSlider = new Slider { Minimum = 20, Maximum = 200, Value = 40 };
			var imageScaleSlider = new Slider { Minimum = 1, Maximum = 20, Value = 1 };

			textEntry.TextChanged += (_, args) =>
			{
				circleLabel.Text = args.NewTextValue;
				roundRectangleLabel.Text = args.NewTextValue;
				triangleLabel.Text = args.NewTextValue;
			};
			fontSizeSlider.ValueChanged += (_, args) =>
			{
				circleLabel.FontSize = args.NewValue;
				roundRectangleLabel.FontSize = args.NewValue;
				triangleLabel.FontSize = args.NewValue;
			};
			imageScaleSlider.ValueChanged += (_, args) =>
			{
				circleImage.Scale = args.NewValue;
				roundRectangleImage.Scale = args.NewValue;
				triangleImage.Scale = args.NewValue;
			};

			var root = new VerticalStackLayout
			{
				Padding = 20,
				Spacing = 10,
				VerticalOptions = LayoutOptions.Center,
				Children =
				{
					grid,
					new Label { Text = "Content Text" },
					textEntry,
					new Label { Text = "Content Text FontSize" },
					fontSizeSlider,
					new Label { Text = "Image Scale" },
					imageScaleSlider
				}
			};
			var page = new ContentPage { Content = root };

			double layoutObservation = -1;
			await CreateHandlerAndAddToWindow<IWindowHandler>(page, async windowHandler =>
			{
				var layoutObserved = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
				grid.Dispatcher.Dispatch(() =>
				{
					layoutObservation = grid.Width;
					layoutObserved.TrySetResult();
				});
				await layoutObserved.Task.WaitAsync(TimeSpan.FromSeconds(5));

				Assert.NotEqual(-1, layoutObservation);
				Assert.Equal(0, Grid.GetColumn(triangleBorder));
				Assert.Equal(0, Grid.GetColumn(roundRectangleBorder));
				Assert.Equal(2, Grid.GetRow(triangleBorder));
				Assert.Equal(1, Grid.GetRow(roundRectangleBorder));

				var nativeWindow = Assert.IsAssignableFrom<UIWindow>(windowHandler.PlatformView);
				Assert.True(nativeWindow.Bounds.Width > 0);
				Assert.True(nativeWindow.Bounds.Height > 0);

				foreach (var border in borders)
				{
					var borderView = Assert.IsAssignableFrom<MContentView>(border.Handler.PlatformView);
					Assert.InRange((double)borderView.Frame.Width, 100, 102);
					Assert.InRange((double)borderView.Frame.Height, 100, 102);
					Assert.Contains(borderView.Subviews, view => view.Tag == MContentView.ContentTag);
				}

				var roundRectangleView = Assert.IsAssignableFrom<MContentView>(roundRectangleBorder.Handler.PlatformView);
				var triangleView = Assert.IsAssignableFrom<MContentView>(triangleBorder.Handler.PlatformView);

				foreach (var image in new[] { circleImage, roundRectangleImage, triangleImage })
				{
					Assert.NotNull(image.Source);
					Assert.IsAssignableFrom<UIImageView>(image.Handler.PlatformView);
				}

				using var roundRectangleBitmap = await roundRectangleView.ToBitmap(MauiContext);
				using var triangleBitmap = await triangleView.ToBitmap(MauiContext);
				Assert.NotNull(roundRectangleBitmap.CGImage);
				Assert.NotNull(triangleBitmap.CGImage);

				var roundScale = (double)roundRectangleBitmap.CGImage.Width / (double)roundRectangleView.Bounds.Width;
				var triangleScale = (double)triangleBitmap.CGImage.Width / (double)triangleView.Bounds.Width;
				Assert.InRange(roundScale, 0.9, 3.1);
				Assert.InRange(triangleScale, 0.9, 3.1);
				Assert.InRange((double)roundRectangleBitmap.CGImage.Height / (double)roundRectangleView.Bounds.Height, roundScale - 0.1, roundScale + 0.1);
				Assert.InRange((double)triangleBitmap.CGImage.Height / (double)triangleView.Bounds.Height, triangleScale - 0.1, triangleScale + 0.1);

				var expectedStroke = new byte[] { 144, 238, 144, 255 };
				const int tolerance = 24;

				int ColorDistance(byte[] actual, byte[] expected) =>
					Math.Abs(actual[0] - expected[0]) +
					Math.Abs(actual[1] - expected[1]) +
					Math.Abs(actual[2] - expected[2]);

				bool IsStroke(byte[] pixel) =>
					Math.Abs(pixel[0] - expectedStroke[0]) <= tolerance &&
					Math.Abs(pixel[1] - expectedStroke[1]) <= tolerance &&
					Math.Abs(pixel[2] - expectedStroke[2]) <= tolerance;

				byte[] PixelAt(UIImage bitmap, double scale, Point point) =>
					bitmap.GetPixel(
						(int)Math.Round(point.X * scale),
						(int)Math.Round(point.Y * scale));

				var roundRectangleSamples = new[]
				{
					new Point(25, 4), new Point(76, 4),
					new Point(25, 97), new Point(76, 97),
					new Point(4, 25), new Point(4, 76),
					new Point(97, 25), new Point(97, 76)
				};
				var roundRectanglePixels = roundRectangleSamples
					.Select(point => PixelAt(roundRectangleBitmap, roundScale, point))
					.ToArray();
				var roundRectangleContaminated = roundRectanglePixels.Count(pixel => !IsStroke(pixel));
				Assert.Equal(0, roundRectangleContaminated);

				var vertices = new[]
				{
					new Point(40, 10),
					new Point(70, 80),
					new Point(10, 50)
				};
				var centroid = new Point(40, 140d / 3d);
				var triangleSamples = new List<Point>();
				for (int index = 0; index < vertices.Length; index++)
				{
					var start = vertices[index];
					var end = vertices[(index + 1) % vertices.Length];
					foreach (var fraction in new[] { 0.3, 0.7 })
					{
						var edgePoint = new Point(
							start.X + ((end.X - start.X) * fraction),
							start.Y + ((end.Y - start.Y) * fraction));
						var towardCenterX = centroid.X - edgePoint.X;
						var towardCenterY = centroid.Y - edgePoint.Y;
						var length = Math.Sqrt((towardCenterX * towardCenterX) + (towardCenterY * towardCenterY));
						triangleSamples.Add(new Point(
							edgePoint.X + (2 * towardCenterX / length),
							edgePoint.Y + (2 * towardCenterY / length)));
					}
				}

				var trianglePixels = triangleSamples
					.Select(point => PixelAt(triangleBitmap, triangleScale, point))
					.ToArray();
				var triangleContaminated = trianglePixels.Count(pixel => !IsStroke(pixel));
				var redOverGreen = new byte[]
				{
					(byte)Math.Round((0.6 * 255) + (0.4 * expectedStroke[0])),
					(byte)Math.Round(0.4 * expectedStroke[1]),
					(byte)Math.Round(0.4 * expectedStroke[2]),
					255
				};
				Assert.True(ColorDistance(redOverGreen, expectedStroke) > tolerance * 3);

				var observedColors = string.Join(
					",",
					trianglePixels.Select(pixel => $"#{pixel[0]:X2}{pixel[1]:X2}{pixel[2]:X2}{pixel[3]:X2}"));
				Assert.True(
					triangleContaminated == 0,
					$"Issue17525 polygon inner clip leaked content into the stroke band; contaminatedPixels={triangleContaminated}, expected=0, sampledPixels={trianglePixels.Length}, tolerance={tolerance}, roundFrame={roundRectangleView.Frame}, triangleFrame={triangleView.Frame}, observedColors={observedColors}");
			});
		}
	}
}
#endif

